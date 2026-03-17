
public class DashBots : MonoBehaviour
{
    
	public float dashSpeed = 12f;                     // = initialVelocity
	public float dashTime = 0.4f;                     // = duration
	public float dashCD = 0f;

    private float currentCD;
    private bool isDashing;

    	private void Update()
	{
		if (!IsLocalPlayer) return;

		if (dashCD > 0f)
		{
			dashCD -= Time.deltaTime;
			if (dashUiObj != null)
				dashUiObj.value -= Time.deltaTime;
		}
		else
		{
			if (dashUiObj != null)
			{
				playerReference.characterBrain.characterActions.dash.value = false;
				Destroy(dashUiObj.gameObject);
			}
		}
	}
    
}