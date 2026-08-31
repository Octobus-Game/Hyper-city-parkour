using UnityEngine;

public class Клейк1_old : MonoBehaviour
{
	[SerializeField]
	private float _speedwalk;

	[SerializeField]
	private float _gravity;

	[SerializeField]
	private float _Jumppower;

	private CharacterController _characterController;

	private Vector3 _walkDirection;

	private Vector3 _velocity;

	private void Start()
	{
		_characterController = GetComponent<CharacterController>();
	}

	private void Update()
	{
		Junmp(_characterController.isGrounded && Input.GetKey(KeyCode.Space));
		float axis = Input.GetAxis("Horizontal");
		float axis2 = Input.GetAxis("Vertical");
		_walkDirection = base.transform.right * axis + base.transform.forward * axis2;
	}

	private void FixedUpdate()
	{
		walk(_walkDirection);
		DoGravity(_characterController.isGrounded);
	}

	private void walk(Vector3 direction)
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

	private void Junmp(bool canJump)
	{
		if (canJump)
		{
			_velocity.y = _Jumppower;
		}
	}
}
