using UnityEngine;

public static class TankOverdriveSetup
{
    private static readonly Vector3 DefaultExhaustLocalPosition = new Vector3(0f, 0.65f, -2.35f);
    private static readonly Vector3 DefaultExhaustLocalEuler = new Vector3(0f, 180f, 0f);

    public static void Configure(GameObject tank)
    {
        if (tank == null)
        {
            return;
        }

        TankOverdriveController overdrive = tank.GetComponent<TankOverdriveController>();
        if (overdrive == null)
        {
            overdrive = tank.AddComponent<TankOverdriveController>();
        }

        overdrive.allowOverdrive = true;
        overdrive.exhaustPoint = EnsureExhaustPoint(tank.transform);

        TankRpmGaugeUI gauge = tank.GetComponent<TankRpmGaugeUI>();
        if (gauge == null)
        {
            gauge = tank.AddComponent<TankRpmGaugeUI>();
        }

        gauge.overdrive = overdrive;
        gauge.showOnlyWhileDriving = true;
    }

    public static Transform EnsureExhaustPoint(Transform tankRoot)
    {
        if (tankRoot == null)
        {
            return null;
        }

        Transform existing = tankRoot.Find("ExhaustPoint");
        if (existing != null)
        {
            return existing;
        }

        GameObject exhaustObject = new GameObject("ExhaustPoint");
        exhaustObject.transform.SetParent(tankRoot, false);
        exhaustObject.transform.localPosition = DefaultExhaustLocalPosition;
        exhaustObject.transform.localRotation = Quaternion.Euler(DefaultExhaustLocalEuler);
        return exhaustObject.transform;
    }
}
