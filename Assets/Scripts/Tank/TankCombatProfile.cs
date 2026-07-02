// Shared turret/combat feel for WAR, Driving Practice, and Cannon Practice.
public static class TankCombatProfile
{
    public const float TurretYawSpeed = 55f;
    public const float MouseYawDegreesPerSecond = 32f;
    public const float MouseWheelPitchSensitivity = 18f;
    public const float KeyboardPitchSpeed = 24f;

    public static void ApplyToTankController(TankController tank)
    {
        if (tank == null)
        {
            return;
        }

        tank.turretYawSpeed = TurretYawSpeed;
        tank.mouseYawDegreesPerSecond = MouseYawDegreesPerSecond;
        tank.mouseWheelPitchSensitivity = MouseWheelPitchSensitivity;
        tank.keyboardPitchSpeed = KeyboardPitchSpeed;

        TankTurretController turret = tank.GetComponent<TankTurretController>();
        if (turret != null && tank.turretYawPivot != null)
        {
            turret.Bind(tank.turretYawPivot, TurretYawSpeed, MouseYawDegreesPerSecond);
        }
    }
}
