using Mirror;
using UnityEngine;

public class CharacterMovement2_old : NetworkBehaviour
{
	[SerializeField]
	private float _speedwalk;

	[SerializeField]
	private float _gravity;

	[SerializeField]
	private float _jumpPower;

	private CharacterController _characterController;

	private Vector3 _walkDirection;

	private Vector3 _velocity;

	private void Start()
	{
		_characterController = GetComponent<CharacterController>();
	}

	private void Update()
	{
		Jump(_characterController.isGrounded && Input.GetKey(KeyCode.Space));
		float num = 0f;
		float num2 = 0f;
		if (Input.GetKey(KeyCode.W))
		{
			num2 = 1f;
		}
		if (Input.GetKey(KeyCode.S))
		{
			num2 = -1f;
		}
		if (Input.GetKey(KeyCode.A))
		{
			num = -1f;
		}
		if (Input.GetKey(KeyCode.D))
		{
			num = 1f;
		}
		_walkDirection = base.transform.right * num + base.transform.forward * num2;
		_ = base.isLocalPlayer;
	}

	private void FixedUpdate()
	{
		Walk(_walkDirection);
		DoGravity(_characterController.isGrounded);
	}

	private void Walk(Vector3 direction)
	{
		_characterController.Move(direction * _speedwalk * Time.fixedDeltaTime);
	}

	private void DoGravity(bool isGrounded)
	{
		if (isGrounded && _velocity.y < 0f)
		{
			_velocity.y = -1f;
		}
		_velocity.y -= _gravity * Time.fixedDeltaTime;
		_characterController.Move(_velocity * Time.fixedDeltaTime);
	}

	private void Jump(bool canJump)
	{
		if (canJump)
		{
			_velocity.y = _jumpPower;
		}
	}

	public override bool Weaved()
	{
		return true;
	}
}
