#if UNITY_EDITOR
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class PlayStartFlowPlayModeTests
{
    [Test]
    public void HumanPlayButton_PreservesPresentedPlayerPose()
    {
        GameObject tankRoot = CreateTank(out TankController tank);
        GameObject apiObject = new GameObject("GameplayTestApi");
        GameplayTestApi api = apiObject.AddComponent<GameplayTestApi>();
        api.playerTank = tank;

        GameObject menuObject = new GameObject("MenuManager");
        MenuManager menu = menuObject.AddComponent<MenuManager>();
        menu.playerTank = tank;
        menu.mainPanel = new GameObject("MainPanel");
        menu.playButton = new GameObject("PlayButton", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
        menu.SendMessage("Start");

        Vector3 positionBefore = tank.transform.position;
        Quaternion rotationBefore = tank.transform.rotation;
        Quaternion turretBefore = tank.turretYawPivot.localRotation;
        Quaternion barrelBefore = tank.barrelPitchPivot.localRotation;

        menu.playButton.onClick.Invoke();

        Assert.That(Vector3.Distance(positionBefore, tank.transform.position), Is.LessThan(0.01f));
        Assert.That(Quaternion.Angle(rotationBefore, tank.transform.rotation), Is.LessThan(0.01f));
        Assert.That(Quaternion.Angle(turretBefore, tank.turretYawPivot.localRotation), Is.LessThan(0.01f));
        Assert.That(Quaternion.Angle(barrelBefore, tank.barrelPitchPivot.localRotation), Is.LessThan(0.01f));
        Assert.That(tank.RigidbodyComponent.IsSleeping(), Is.True);
        Assert.That(tank.RigidbodyComponent.isKinematic, Is.True);

        Object.DestroyImmediate(menu.playButton.gameObject);
        Object.DestroyImmediate(menu.mainPanel);
        Object.DestroyImmediate(menuObject);
        Object.DestroyImmediate(apiObject);
        Object.DestroyImmediate(tankRoot);
    }

    [Test]
    public void Defeat_DisablesPlayerInputAndRemainsResolved()
    {
        GameObject tankRoot = CreateTank(out TankController tank);
        GameObject directorObject = new GameObject("BattlefieldDirector");
        BattlefieldDirector director = directorObject.AddComponent<BattlefieldDirector>();
        director.playerTank = tank.transform;
        director.BeginBattle(1);

        director.ForceDefeat("Regression test");

        Assert.That(director.State, Is.EqualTo(BattlefieldDirector.BattleState.Defeat));
        Assert.That(tank.enabled, Is.False);
        director.SendMessage("Update");
        Assert.That(director.State, Is.EqualTo(BattlefieldDirector.BattleState.Defeat));

        Object.DestroyImmediate(directorObject);
        Object.DestroyImmediate(tankRoot);
    }

    [Test]
    public void PrepareForGameplay_StartupSettlingDoesNotTriggerRolloverDefeat()
    {
        GameObject tankRoot = CreateTank(out TankController tank);
        tank.rolloverDefeatDelay = 0f;
        tank.PrepareForGameplay();
        tank.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

        bool defeated = (bool)typeof(TankController)
            .GetMethod("CheckRolloverDefeat", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(tank, null);

        Assert.That(defeated, Is.False);
        Assert.That(tank.IsDestroyed, Is.False);
        Object.DestroyImmediate(tankRoot);
    }

    [Test]
    public void DeterministicOneEnemyBattle_CanReachVictory()
    {
        GameObject directorObject = new GameObject("BattlefieldDirector");
        BattlefieldDirector director = directorObject.AddComponent<BattlefieldDirector>();
        director.requireCastleSurvival = false;
        director.victoryDelay = 0f;
        director.BeginBattle(1);

        GameObject enemyObject = new GameObject("EnemyTank");
        EnemyTankAI enemy = enemyObject.AddComponent<EnemyTankAI>();
        director.RegisterEnemySpawned(enemy);
        Object.DestroyImmediate(enemyObject);
        director.SendMessage("Update");

        Assert.That(director.State, Is.EqualTo(BattlefieldDirector.BattleState.Victory));
        Object.DestroyImmediate(directorObject);
    }

    private static GameObject CreateTank(out TankController tank)
    {
        GameObject root = new GameObject("PlayerTank");
        root.transform.SetPositionAndRotation(new Vector3(3f, 5f, -7f), Quaternion.Euler(4f, 12f, -2f));
        root.AddComponent<Rigidbody>();
        root.AddComponent<BoxCollider>();

        Transform turret = new GameObject("TurretYawPivot").transform;
        turret.SetParent(root.transform, false);
        turret.localRotation = Quaternion.Euler(0f, 90f, 0f);
        Transform barrel = new GameObject("BarrelPitchPivot").transform;
        barrel.SetParent(turret, false);
        barrel.localRotation = Quaternion.Euler(-2f, 0f, 0f);
        Transform firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(barrel, false);

        tank = root.AddComponent<TankController>();
        tank.turretYawPivot = turret;
        tank.barrelPitchPivot = barrel;
        tank.cannonFirePoint = firePoint;
        return root;
    }
}
#endif
