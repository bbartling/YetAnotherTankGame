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
    public void GameplayTestApi_ExposesEnemyOperationalEvidence()
    {
        Assert.That(typeof(GameplayTestApi).GetProperty("EnemyDistanceToPlayer", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(GameplayTestApi).GetProperty("EnemyHasLineOfSight", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(GameplayTestApi).GetProperty("EnemyCurrentState", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
    }

    [Test]
    public void EnemySpawner_UsesSafeHullClearanceAboveGround()
    {
        Assert.That(EnemyTankSpawner.GetSafeGroundLift(0.35f), Is.GreaterThanOrEqualTo(2.5f));
        Assert.That(EnemyTankSpawner.GetSafeGroundLift(4f), Is.EqualTo(4f));
    }
}
