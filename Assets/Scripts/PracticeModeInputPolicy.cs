using UnityEngine;

[DisallowMultipleComponent]
public class PracticeModeInputPolicy : MonoBehaviour
{
    public enum PracticeControlMode
    {
        War,
        DrivingOnly,
        TargetPractice
    }

    public PracticeControlMode mode = PracticeControlMode.War;
    public TankController playerTank;
    public bool applyOnStart = true;

    public bool AllowsDriving => mode != PracticeControlMode.TargetPractice;
    public bool AllowsTurret => mode != PracticeControlMode.DrivingOnly;
    public bool AllowsCannon => mode != PracticeControlMode.DrivingOnly;
    public bool AllowsScope => mode == PracticeControlMode.TargetPractice || mode == PracticeControlMode.War;
    public bool AllowsMachineGun => mode == PracticeControlMode.War;

    private void Start()
    {
        if (applyOnStart)
        {
            ApplyNow();
        }
    }

    [ContextMenu("Apply Practice Input Policy")]
    public void ApplyNow()
    {
        if (playerTank == null)
        {
            playerTank = Object.FindAnyObjectByType<TankController>();
        }

        ApplyTo(playerTank);
    }

    public void ApplyTo(TankController tank)
    {
        if (tank == null)
        {
            return;
        }

        tank.allowDrivingInput = AllowsDriving;
        tank.allowTurretInput = AllowsTurret;
        tank.allowCannonInput = AllowsCannon;
        tank.allowPowerInput = AllowsCannon;
        tank.disableRolloverDefeat = mode == PracticeControlMode.DrivingOnly;

        TankMachineGun machineGun = tank.GetComponent<TankMachineGun>();
        if (machineGun != null)
        {
            machineGun.allowInputFire = AllowsMachineGun;
        }

        SniperRangeFinder rangeFinder = tank.GetComponent<SniperRangeFinder>();
        if (rangeFinder != null)
        {
            rangeFinder.allowScope = AllowsScope;
        }

        Camera gameplayCamera = tank.gameplayCamera != null ? tank.gameplayCamera : tank.GetComponentInChildren<Camera>(true);
        if (gameplayCamera != null)
        {
            TankBarrelScopeCamera scopeCamera = gameplayCamera.GetComponent<TankBarrelScopeCamera>();
            if (scopeCamera != null)
            {
                scopeCamera.allowScope = AllowsScope;
            }
        }
    }
}
