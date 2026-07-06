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
        Assert.That(tank.RigidbodyComponent.isKinematic, Is.False);

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
    public void PrepareForGameplay_SettlesHighSpawnOntoGroundBeforeDriveInput()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(20f, 1f, 20f);

        GameObject tankRoot = CreateTank(out TankController tank);
        tankRoot.transform.position = new Vector3(0f, 8f, 0f);
        tank.terrainSurfaceSkin = 0.08f;

        tank.PrepareForGameplay();

        Collider tankCollider = tankRoot.GetComponent<Collider>();
        float groundTop = ground.GetComponent<Collider>().bounds.max.y;
        float clearance = tankCollider.bounds.min.y - groundTop;

        Assert.That(clearance, Is.GreaterThanOrEqualTo(0.06f));
        Assert.That(clearance, Is.LessThan(0.2f));
        Assert.That(tank.RigidbodyComponent.isKinematic, Is.False);

        Object.DestroyImmediate(tankRoot);
        Object.DestroyImmediate(ground);
    }

    [Test]
    public void PrepareForGameplay_CanBeCalledTwiceWithoutKinematicVelocityErrors()
    {
        GameObject tankRoot = CreateTank(out TankController tank);

        tank.PrepareForGameplay();
        tank.PrepareForGameplay();

        Assert.That(tank.RigidbodyComponent.isKinematic, Is.False);

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

    [Test]
    public void MenuModes_ExposeDrivingCannonAndWarStarts()
    {
        MenuManager menu = new GameObject("MenuManager").AddComponent<MenuManager>();

        Assert.That(typeof(MenuManager).GetMethod("StartDrivingPractice"), Is.Not.Null);
        Assert.That(typeof(MenuManager).GetMethod("StartCannonPractice"), Is.Not.Null);
        Assert.That(typeof(MenuManager).GetMethod("StartWar"), Is.Not.Null);
        Assert.That(System.Enum.GetNames(typeof(MenuManager.GameplayMode)), Does.Contain("DrivingPractice"));
        Assert.That(System.Enum.GetNames(typeof(MenuManager.GameplayMode)), Does.Contain("CannonPractice"));
        Assert.That(System.Enum.GetNames(typeof(MenuManager.GameplayMode)), Does.Contain("War"));

        Object.DestroyImmediate(menu.gameObject);
    }

    [Test]
    public void AuthoredMenuButtons_DoNotCreateRuntimeMenuPresentationObjects()
    {
        GameObject menuObject = new GameObject("MenuManager");
        MenuManager menu = menuObject.AddComponent<MenuManager>();
        menu.mainPanel = new GameObject("MainPanel", typeof(RectTransform));
        menu.drivingPracticeButton = CreateButton("DrivingPracticeButton", menu.mainPanel.transform);
        menu.cannonPracticeButton = CreateButton("CannonPracticeButton", menu.mainPanel.transform);
        menu.warButton = CreateButton("WarButton", menu.mainPanel.transform);
        menu.playButton = menu.warButton;

        menu.SendMessage("Start");

        Assert.That(menu.mainPanel.transform.Find("InstructionsText"), Is.Null);
        Assert.That(menu.mainPanel.transform.Find("ModeTutorialText"), Is.Null);
        Assert.That(menu.mainPanel.transform.Find("EnemyCountPanel"), Is.Null);
        Assert.That(menu.mainPanel.transform.Find("DrivingPracticeButton"), Is.Not.Null);
        Assert.That(menu.mainPanel.transform.Find("CannonPracticeButton"), Is.Not.Null);
        Assert.That(menu.mainPanel.transform.Find("WarButton"), Is.Not.Null);

        Object.DestroyImmediate(menu.mainPanel);
        Object.DestroyImmediate(menuObject);
    }

    [Test]
    public void PracticeModes_DoNotSpawnEnemyWaveButWarDoes()
    {
        GameObject tankRoot = CreateTank(out TankController tank);
        GameObject apiObject = new GameObject("GameplayTestApi");
        GameplayTestApi api = apiObject.AddComponent<GameplayTestApi>();
        api.playerTank = tank;
        CountingEnemySpawner spawner = apiObject.AddComponent<CountingEnemySpawner>();
        api.enemySpawner = spawner;

        api.StartMode(MenuManager.GameplayMode.DrivingPractice, 7);
        Assert.That(spawner.SpawnCalls, Is.EqualTo(0));
        Assert.That(tank.enabled, Is.True);

        api.StartMode(MenuManager.GameplayMode.CannonPractice, 7);
        Assert.That(spawner.SpawnCalls, Is.EqualTo(0));

        api.StartMode(MenuManager.GameplayMode.War, 7);
        Assert.That(spawner.SpawnCalls, Is.EqualTo(1));
        Assert.That(spawner.LastEnemyCount, Is.EqualTo(7));

        Object.DestroyImmediate(apiObject);
        Object.DestroyImmediate(tankRoot);
    }

    [Test]
    public void DefeatLeftClick_RestartsCurrentGameplayMode()
    {
        GameObject tankRoot = CreateTank(out TankController tank);
        GameObject apiObject = new GameObject("GameplayTestApi");
        GameplayTestApi api = apiObject.AddComponent<GameplayTestApi>();
        api.playerTank = tank;
        CountingEnemySpawner spawner = apiObject.AddComponent<CountingEnemySpawner>();
        api.enemySpawner = spawner;

        api.StartMode(MenuManager.GameplayMode.War, 3);
        spawner.SpawnCalls = 0;

        api.RestartCurrentMode();

        Assert.That(api.CurrentMode, Is.EqualTo(MenuManager.GameplayMode.War));
        Assert.That(spawner.SpawnCalls, Is.EqualTo(1));
        Assert.That(spawner.LastEnemyCount, Is.EqualTo(3));
        Assert.That(tank.enabled, Is.True);

        Object.DestroyImmediate(apiObject);
        Object.DestroyImmediate(tankRoot);
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

    private static Button CreateButton(string name, Transform parent)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        return buttonObject.GetComponent<Button>();
    }

    private sealed class CountingEnemySpawner : EnemyTankSpawner
    {
        public int SpawnCalls;
        public int LastEnemyCount;

        public override void SpawnEnemyTanks(int count)
        {
            SpawnCalls++;
            LastEnemyCount = count;
        }
    }
}
#endif
