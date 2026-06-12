using NUnit.Framework;
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
}
