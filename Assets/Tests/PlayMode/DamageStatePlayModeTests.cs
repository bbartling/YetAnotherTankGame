#if UNITY_EDITOR
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class DamageStatePlayModeTests
{
    [Test]
    public void DamageStateController_ProgressesThroughVisibleTankStates()
    {
        GameObject root = new GameObject("DamageStateTank");
        GameObject smoke = new GameObject("Smoke");
        GameObject fire = new GameObject("Fire");
        GameObject wreck = new GameObject("Wreck");
        smoke.transform.SetParent(root.transform);
        fire.transform.SetParent(root.transform);
        wreck.transform.SetParent(root.transform);
        DamageStateController states = root.AddComponent<DamageStateController>();
        states.smokeEffect = smoke;
        states.fireEffect = fire;
        states.wreckModel = wreck;

        states.ApplyHealthRatio(0.7f);
        Assert.That(states.CurrentState, Is.EqualTo(DamageStateController.State.Smoking));
        Assert.That(smoke.activeSelf, Is.True);

        states.ApplyHealthRatio(0.3f);
        Assert.That(states.CurrentState, Is.EqualTo(DamageStateController.State.BurningDisabled));
        Assert.That(fire.activeSelf, Is.True);

        states.ApplyHealthRatio(0f);
        Assert.That(states.CurrentState, Is.EqualTo(DamageStateController.State.Wrecked));
        Assert.That(wreck.activeSelf, Is.True);

        Object.DestroyImmediate(root);
    }

    [Test]
    public void DamageableAndZone_ApplyConfiguredDamageMultiplier()
    {
        GameObject root = new GameObject("DamageableTank");
        Damageable damageable = root.AddComponent<Damageable>();
        damageable.maxHealth = 100f;
        damageable.ResetHealth();
        DamageZone zone = root.AddComponent<DamageZone>();
        zone.damageable = damageable;
        zone.damageMultiplier = 1.5f;

        zone.ApplyDamage(20f);

        Assert.That(damageable.CurrentHealth, Is.EqualTo(70f).Within(0.001f));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void DamageZones_ExposeTrackTurretAndBarrelPenalties()
    {
        GameObject root = new GameObject("DamageZones");
        DamageZone track = root.AddComponent<DamageZone>();
        track.zoneType = DamageZone.ZoneType.Track;
        track.ApplyDamage(75f);
        Assert.That(track.MovementMultiplier, Is.LessThan(0.7f));

        DamageZone turret = root.AddComponent<DamageZone>();
        turret.zoneType = DamageZone.ZoneType.Turret;
        turret.ApplyDamage(75f);
        Assert.That(turret.TurretRotationMultiplier, Is.LessThan(0.7f));

        DamageZone barrel = root.AddComponent<DamageZone>();
        barrel.zoneType = DamageZone.ZoneType.Barrel;
        barrel.ApplyDamage(75f);
        Assert.That(barrel.AccuracyMultiplier, Is.LessThan(0.8f));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void CastleAndTree_ExposeDamageStateEvidence()
    {
        GameObject castleObject = new GameObject("Castle");
        CastleDamageReceiver castle = castleObject.AddComponent<CastleDamageReceiver>();
        castle.ApplyImpact(Vector3.zero, Vector3.up, castle.cannonImpactThreshold);
        Assert.That(castle.CurrentDamageState, Is.Not.EqualTo("Intact"));

        GameObject treeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        treeObject.AddComponent<Rigidbody>();
        BreakableTree tree = treeObject.AddComponent<BreakableTree>();
        tree.ApplyImpact(Vector3.zero, Vector3.up, tree.shellCollisionDamage, false);
        Assert.That(tree.IsBroken, Is.True);

        Object.DestroyImmediate(castleObject);
        if (treeObject != null) Object.DestroyImmediate(treeObject);
    }

    [Test]
    public void Tree_TankImpactCanFlattenImmediatelyWithFallbackSound()
    {
        GameObject treeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        treeObject.AddComponent<Rigidbody>();
        BreakableTree tree = treeObject.AddComponent<BreakableTree>();
        tree.smashSound = null;
        tree.tankFlattenDamage = 999f;

        tree.ApplyImpact(Vector3.zero, Vector3.up, tree.tankFlattenDamage, true);

        Assert.That(tree.IsBroken, Is.True);
        Assert.That(tree.SmashClipForTest, Is.Not.Null);
        Assert.That(tree.SmashClipForTest.name, Does.Contain("Tree"));

        if (treeObject != null) Object.DestroyImmediate(treeObject);
    }

    [Test]
    public void TreeFieldSpawner_GeneratesFlattenableTreesWithFallbackSounds()
    {
        GameObject terrainObject = new GameObject("TreeAuditTerrain");
        terrainObject.SetActive(false);
        terrainObject.AddComponent<MeshFilter>();
        terrainObject.AddComponent<MeshRenderer>();
        terrainObject.AddComponent<MeshCollider>();
        CraterTerrain terrain = terrainObject.AddComponent<CraterTerrain>();
        terrain.xSegments = 8;
        terrain.zSegments = 8;
        terrain.terrainWidth = 160f;
        terrain.terrainLength = 160f;
        terrain.seedRandomCraters = false;
        terrain.seedRandomHills = false;
        terrain.randomizeSeedEachRun = false;
        terrainObject.SetActive(true);

        GameObject spawnerObject = new GameObject("TreeAuditSpawner");
        TreeFieldSpawner spawner = spawnerObject.AddComponent<TreeFieldSpawner>();
        spawner.generateOnAwake = false;
        spawner.terrainSource = terrain;
        spawner.treeCount = 6;
        spawner.clearRadiusFromPlayer = 0f;
        spawner.clearRadiusFromCastle = 0f;
        spawner.treeSmashSound = null;

        spawner.SpawnTrees();

        BreakableTree[] trees = spawnerObject.GetComponentsInChildren<BreakableTree>(true);
        Assert.That(trees.Length, Is.EqualTo(6));
        for (int i = 0; i < trees.Length; i++)
        {
            Assert.That(trees[i].SmashClipForTest, Is.Not.Null);
            Assert.That(trees[i].tankFlattenDamage, Is.LessThanOrEqualTo(trees[i].maxHealth * 1.5f));
        }

        Object.DestroyImmediate(spawnerObject);
        Object.DestroyImmediate(terrainObject);
    }

    [Test]
    public void EffectPool_DeactivatesOldestBeyondBudget()
    {
        GameObject root = new GameObject("EffectPool");
        EffectPool pool = root.AddComponent<EffectPool>();
        pool.maxActiveEffects = 2;
        GameObject first = new GameObject("First");
        GameObject second = new GameObject("Second");
        GameObject third = new GameObject("Third");

        pool.Register(first);
        pool.Register(second);
        pool.Register(third);

        Assert.That(pool.ActiveCount, Is.EqualTo(2));
        Assert.That(first.activeSelf, Is.False);

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(first);
        Object.DestroyImmediate(second);
        Object.DestroyImmediate(third);
    }

    [Test]
    public void EnemyTankDamage_UpdatesModularDamageState()
    {
        GameObject enemyObject = new GameObject("DamageStateEnemy");
        enemyObject.AddComponent<Rigidbody>();
        enemyObject.AddComponent<AudioSource>();
        DamageStateController states = enemyObject.AddComponent<DamageStateController>();
        EnemyTankAI enemy = enemyObject.AddComponent<EnemyTankAI>();

        enemy.ApplyMachineGunDamage(40f, Vector3.zero, Vector3.up);

        Assert.That(states.CurrentState, Is.EqualTo(DamageStateController.State.Smoking));
        Assert.That(enemy.CurrentDamageState, Is.EqualTo("Smoking"));
        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void EnemyTankDeath_UsesRandomizedRendererBurstInsteadOfOrderedLoop()
    {
        GameObject enemyObject = new GameObject("RandomDeathEnemy");
        enemyObject.AddComponent<Rigidbody>();
        enemyObject.AddComponent<AudioSource>();
        EnemyTankAI enemy = enemyObject.AddComponent<EnemyTankAI>();

        Assert.That(enemy.randomizeDeathRendererBursts, Is.True);
        Assert.That(enemy.deathRendererBatchMin, Is.GreaterThan(1));
        Assert.That(enemy.deathRendererBatchMax, Is.GreaterThanOrEqualTo(enemy.deathRendererBatchMin));

        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void ModelSwap_ActivatesCorrectDamageVariant()
    {
        GameObject root = new GameObject("ModelSwap");
        GameObject intact = new GameObject("Intact");
        GameObject damaged = new GameObject("Damaged");
        GameObject destroyed = new GameObject("Destroyed");
        DestructibleModelSwap swap = root.AddComponent<DestructibleModelSwap>();
        swap.intactModel = intact;
        swap.damagedModel = damaged;
        swap.destroyedModel = destroyed;

        swap.ApplyState(DamageStateController.State.BurningDisabled);
        Assert.That(damaged.activeSelf, Is.True);
        Assert.That(intact.activeSelf, Is.False);

        swap.ApplyState(DamageStateController.State.Wrecked);
        Assert.That(destroyed.activeSelf, Is.True);

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(intact);
        Object.DestroyImmediate(damaged);
        Object.DestroyImmediate(destroyed);
    }

    [Test]
    public void CraterBudget_RejectsUnboundedRuntimeImpacts()
    {
        Assert.That(CraterTerrain.IsWithinRuntimeImpactBudget(0, 2), Is.True);
        Assert.That(CraterTerrain.IsWithinRuntimeImpactBudget(1, 2), Is.True);
        Assert.That(CraterTerrain.IsWithinRuntimeImpactBudget(2, 2), Is.False);
    }

    [Test]
    public void PlayerTank_ExposesModularDamageStateEvidence()
    {
        Assert.That(typeof(TankController).GetProperty("CurrentDamageState", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
    }

    [Test]
    public void DamageStateController_CreatesBoundedDefaultSmokeAndFire()
    {
        GameObject root = new GameObject("DefaultDamageEffects");
        DamageStateController states = root.AddComponent<DamageStateController>();

        states.EnsureDefaultEffects();

        Assert.That(states.smokeEffect, Is.Not.Null);
        Assert.That(states.fireEffect, Is.Not.Null);
        Assert.That(states.smokeEffect.GetComponent<ParticleSystem>().main.maxParticles, Is.LessThanOrEqualTo(32));
        Assert.That(states.fireEffect.GetComponent<ParticleSystem>().main.maxParticles, Is.LessThanOrEqualTo(32));

        ParticleSystemRenderer fireRenderer = states.fireEffect.GetComponent<ParticleSystemRenderer>();
        Assert.That(fireRenderer, Is.Not.Null);
        Assert.That(fireRenderer.sharedMaterial, Is.Not.Null);
        Assert.That(fireRenderer.sharedMaterial.shader, Is.Not.Null);
        Assert.That(fireRenderer.sharedMaterial.shader.name, Does.Not.Contain("Error"));

        Object.DestroyImmediate(root);
    }
}
#endif
