using System.Collections;
using UnityEngine;

public class CharacterMovement1_OLD : MonoBehaviour
{
	[SerializeField]
	private float _speedwalk;

	[SerializeField]
	private float _dashSpeedMultiplier = 2f;

	[SerializeField]
	private float _dashDuration = 4f;

	[SerializeField]
	private float _gravity;

	[SerializeField]
	private float _jumpPower;

	[SerializeField]
	private ParticleSystem _dashEffect;

	private CharacterController _characterController;

	private Vector3 _walkDirection;

	private Vector3 _velocity;

	private bool _isDashing;

	private void Start()
	{
		_characterController = GetComponent<CharacterController>();
	}

	private void Update()
	{
		Jump(_characterController.isGrounded && Input.GetKey(KeyCode.Space));
		float num = 0f;
		float num2 = 0f;
		if (Input.GetKey(KeyCode.D))
		{
			num2 = 1f;
		}
		if (Input.GetKey(KeyCode.A))
		{
			num2 = -1f;
		}
		if (Input.GetKey(KeyCode.W))
		{
			num = -1f;
		}
		if (Input.GetKey(KeyCode.S))
		{
			num = 1f;
		}
		if (Input.GetKey(KeyCode.LeftAlt))
		{
			num2 = -1f;
		}
		if (Input.GetKeyDown(KeyCode.LeftControl) && !_isDashing)
		{
			StartCoroutine(Dash());
		}
		_walkDirection = base.transform.right * num + base.transform.forward * num2;
	}

	private void FixedUpdate()
	{
		Walk(_walkDirection);
		DoGravity(_characterController.isGrounded);
	}

	private void Walk(Vector3 direction)
	{
		float num = (_isDashing ? (_speedwalk * _dashSpeedMultiplier) : _speedwalk);
		_characterController.Move(direction * num * Time.fixedDeltaTime);
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

	private IEnumerator Dash()
	{
		_isDashing = true;
		_dashEffect.Play();
		yield return new WaitForSeconds(_dashDuration);
		_isDashing = false;
		_dashEffect.Stop();
	}
}
