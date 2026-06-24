using UnityEngine;

[DisallowMultipleComponent]
public class MainMenuBackdrop : MonoBehaviour
{
  public Transform displayTankRoot;
  public float tankYawSpeed = 8f;
  public float tankBobAmplitude = 0.08f;
  public float tankBobSpeed = 0.7f;

  private Vector3 _restPosition;

  private void Start()
  {
    if (displayTankRoot == null)
    {
      GameObject tankRoot = new GameObject("MenuDisplayTank");
      tankRoot.transform.SetParent(transform, false);
      tankRoot.transform.localPosition = new Vector3(0f, 0.35f, 2.8f);
      tankRoot.transform.localRotation = Quaternion.Euler(0f, 135f, 0f);
      displayTankRoot = tankRoot.transform;
    }

    if (displayTankRoot.childCount == 0)
    {
      SillyModelInstaller.Ensure(displayTankRoot.gameObject, "Models/Tanks/SillyPlayerTank", 3f, false);
    }

    _restPosition = displayTankRoot.localPosition;
  }

  private void Update()
  {
    if (displayTankRoot == null)
    {
      return;
    }

    displayTankRoot.Rotate(0f, tankYawSpeed * Time.deltaTime, 0f, Space.Self);
    float bob = Mathf.Sin(Time.time * tankBobSpeed * Mathf.PI * 2f) * tankBobAmplitude;
    displayTankRoot.localPosition = _restPosition + new Vector3(0f, bob, 0f);
  }
}
