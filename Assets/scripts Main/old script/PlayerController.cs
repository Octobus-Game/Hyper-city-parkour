using UnityEngine;

public class PlayerController_old : MonoBehaviour
{
	public float playerSpeed;

	public float DeathDistance;

	private Vector3 StartPos;

	private bool endGame;

	private void Start()
	{
		StartPos = base.transform.position;
	}

	private void Update()
	{
		if (base.transform.position.y < DeathDistance)
		{
			base.transform.position = StartPos;
		}
		if (!endGame)
		{
			MovePlayer();
		}
	}

	private void MovePlayer()
	{
		if (Input.GetAxis("Horizontal") != 0f || Input.GetAxis("Vertical") != 0f)
		{
			Vector3 normalized = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")).normalized;
			base.transform.Translate(normalized * Time.deltaTime * playerSpeed, Space.World);
			base.transform.LookAt(normalized + base.transform.position);
		}
	}

	public void EndGame()
	{
		endGame = true;
	}
}
