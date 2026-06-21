#if UNITY_EDITOR
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class EnemyTankOperationsPlayModeTests
{
    [Test]
    public void EnemyTank_DefaultsFavorStandoffCombat()
    {
        GameObject enemyObject = new GameObject("PolicyEnemyTank");
        enemyObject.AddComponent<Rigidbody>();
        enemyObject.AddComponent<AudioSource>();
        EnemyTankAI enemy = enemyObject.AddComponent<EnemyTankAI>();

        Assert.That(enemy.preferredDistance, Is.GreaterThanOrEqualTo(160f));
        Assert.That(enemy.retreatDistance, Is.GreaterThanOrEqualTo(90f));
        Assert.That(enemy.fireCooldown, Is.GreaterThanOrEqualTo(6f));
        Assert.That(enemy.strafeForce, Is.Zero);

        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void EnemyTank_DefaultsUseDeliberateMovementAndAim()
    {
        GameObject enemyObject = new GameObject("PolicyEnemyTank");
        enemyObject.AddComponent<Rigidbody>();
        enemyObject.AddComponent<AudioSource>();
        EnemyTankAI enemy = enemyObject.AddComponent<EnemyTankAI>();

        Assert.That(enemy.moveForce, Is.LessThanOrEqualTo(30f));
        Assert.That(enemy.patrolMoveForce, Is.LessThanOrEqualTo(25f));
        Assert.That(enemy.aimSpeed, Is.LessThanOrEqualTo(1.5f));

        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void EnemyTank_ClampsLegacySerializedRushValuesOnEnable()
    {
        GameObject enemyObject = new GameObject("LegacyRushEnemy");
        enemyObject.SetActive(false);
        enemyObject.AddComponent<Rigidbody>();
        enemyObject.AddComponent<AudioSource>();
        EnemyTankAI enemy = enemyObject.AddComponent<EnemyTankAI>();
        enemy.preferredDistance = 130f;
        enemy.retreatDistance = 42f;
        enemy.moveForce = 92f;
        enemy.strafeForce = 28f;
        enemy.fireCooldown = 2.1f;
        enemy.aimSpeed = 3.75f;

        enemyObject.SetActive(true);

        Assert.That(enemy.preferredDistance, Is.GreaterThanOrEqualTo(160f));
        Assert.That(enemy.retreatDistance, Is.GreaterThanOrEqualTo(90f));
        Assert.That(enemy.moveForce, Is.LessThanOrEqualTo(30f));
        Assert.That(enemy.strafeForce, Is.Zero);
        Assert.That(enemy.fireCooldown, Is.GreaterThanOrEqualTo(6f));
        Assert.That(enemy.aimSpeed, Is.LessThanOrEqualTo(1.5f));

        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void EnemyTank_UsesOperationalStatePolicy()
    {
        GameObject enemyObject = new GameObject("OperationalEnemy");
        enemyObject.AddComponent<Rigidbody>();
        enemyObject.AddComponent<AudioSource>();
        EnemyTankAI enemy = enemyObject.AddComponent<EnemyTankAI>();

        Assert.That(enemy.EvaluateOperationalState(true, 220f, 3f, false, false, false), Is.EqualTo("HaltToAim"));
        Assert.That(enemy.EvaluateOperationalState(true, 220f, 0f, true, false, false), Is.EqualTo("Firing"));
        Assert.That(enemy.EvaluateOperationalState(true, 70f, 0f, true, false, false), Is.EqualTo("Retreating"));
        Assert.That(enemy.EvaluateOperationalState(false, 220f, 0f, false, false, true), Is.EqualTo("Suspicious"));

        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void EnemyPathing_ChoosesSideRouteWhenForwardSlopeIsTooSteep()
    {
        TankPathingBrain pathing = new TankPathingBrain { MaxSlopeDegrees = 35f };
        MethodInfo method = typeof(TankPathingBrain).GetMethod("ChooseSlopeAwareDirection", BindingFlags.Instance | BindingFlags.Public);

        Assert.That(method, Is.Not.Null);
        Vector3 chosen = (Vector3)method.Invoke(pathing, new object[] { Vector3.forward, Vector3.right, 48f, 18f, 52f });

        Assert.That(Vector3.Dot(chosen.normalized, -Vector3.right), Is.GreaterThan(0.65f));
    }

    [Test]
    public void GameplayTestApi_ExposesEnemyOperationalEvidence()
    {
        Assert.That(typeof(GameplayTestApi).GetProperty("EnemyDistanceToPlayer", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(GameplayTestApi).GetProperty("EnemyHasLineOfSight", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(GameplayTestApi).GetProperty("EnemyCurrentState", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
    }

    [Test]
    public void EnemySpawner_UsesSafeHullClearanceAboveGround()
    {
        Assert.That(EnemyTankSpawner.GetSafeGroundLift(0.35f), Is.InRange(0.35f, 0.85f));
        Assert.That(EnemyTankSpawner.GetSafeGroundLift(4f), Is.EqualTo(4f));
    }

    [Test]
    public void EnemySpawner_CalculatesGroundSnapFromColliderBottom()
    {
        float delta = EnemyTankSpawner.CalculateGroundSnapDelta(-0.4f, 0f, 0.05f);

        Assert.That(delta, Is.EqualTo(0.45f).Within(0.001f));
    }

    [Test]
    public void EnemyHealthBar_UsesGreenRemainingHealthOverRedDamageBackground()
    {
        GameObject enemyObject = new GameObject("HealthBarEnemy");
        EnemyHealthBar healthBar = enemyObject.AddComponent<EnemyHealthBar>();

        Assert.That(healthBar.healthyColor.g, Is.GreaterThan(healthBar.healthyColor.r));
        Assert.That(healthBar.damageBackgroundColor.r, Is.GreaterThan(healthBar.damageBackgroundColor.g));
        Object.DestroyImmediate(enemyObject);
    }
}
#endif
