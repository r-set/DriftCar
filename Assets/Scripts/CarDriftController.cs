using System.Collections.Generic;
using UnityEngine;

public class CarDriftController : MonoBehaviour
{
    [Header("Drive Settings")]
    [SerializeField] private ParticleSystem _carSmokeEffect;
    [Range(1200f, 2500f)][SerializeField] private float _carMass = 1600f;
    [Range(0f, 200f)][SerializeField] private float _carAcceleration = 60f;
    [Range(0f, 300f)][SerializeField] private float _maxCarSpeed = 180f;
    [Range(0f, 45f)][SerializeField] private float _steeringAngle = 10f;
    [Range(0.9f, 1f)][SerializeField] private float _dragFactor = 0.99f;
    [Range(0.5f, 1f)][SerializeField] private float _tractionFactor = 0.9f;

    [Header("Wheel Settings")]
    [SerializeField] private List<Transform> _wheels;
    [SerializeField] private ParticleSystem _wheelSmokeEffect;
    private const float _degreesPerRevolution = 360f;
    [Range(0.1f, 1f)][SerializeField] private float _wheelRadius = 0.35f;
    [Range(0f, 1f)][SerializeField] private float _wheelSmokeSteerThreshold = 0.6f;

    [Header("Trail Settings")]
    [SerializeField] private List<TrailRenderer> _regularTrails;
    [SerializeField] private List<TrailRenderer> _driftTrails;

    private Rigidbody _carRigidbody;
    private float _steerInput;
    private float _wheelRotationSpeed;
    private bool _isMoving = false;
    private bool _isDrifting = false;
    private bool _isSpinning = false;

    private void Start()
    {
        _carRigidbody = GetComponent<Rigidbody>();
        _carRigidbody.mass = _carMass;
    }

    private void FixedUpdate()
    {
        HandleInput();
        UpdateMovementStatus();
        HandleMovement();
        RotateWheels();
        HandleSmokeEffects();
        UpdateTrailRenderer();
    }

    private void HandleInput()
    {
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");
        _steerInput = horizontalInput;

        float desiredSpeed = _carAcceleration * verticalInput;
        _carRigidbody.velocity = transform.forward * desiredSpeed;

        if (_isMoving)
        {
            float turnAngle = _steeringAngle * horizontalInput;
            Quaternion turnRotation = Quaternion.Euler(0f, turnAngle, 0f);
            _carRigidbody.MoveRotation(_carRigidbody.rotation * turnRotation);
        }

        _carRigidbody.velocity *= _dragFactor;
        _carRigidbody.velocity = Vector3.ClampMagnitude(_carRigidbody.velocity, _maxCarSpeed);

        Vector3 forwardVelocity = Vector3.Project(_carRigidbody.velocity, transform.forward);
        _carRigidbody.velocity = Vector3.Lerp(forwardVelocity, transform.forward * forwardVelocity.magnitude, _tractionFactor * Time.deltaTime);

        _isSpinning = Mathf.Abs(verticalInput) > 0.5f && Mathf.Abs(horizontalInput) > 0.5f;
    }

    private void UpdateMovementStatus()
    {
        _isMoving = _carRigidbody.velocity.magnitude > 0.1f;
    }

    private void HandleMovement()
    {
        if (_isDrifting)
        {
            Vector3 driftForce = -transform.right * _carAcceleration * _steerInput;
            _carRigidbody.AddForce(driftForce);
        }

        if (_isSpinning)
        {
            Vector3 spinForce = transform.right * _carAcceleration * _steerInput;
            _carRigidbody.AddForce(spinForce);
        }
    }

    private void RotateWheels()
    {
        if (_isMoving)
        {
            _wheelRotationSpeed = (_carRigidbody.velocity.magnitude / (2 * Mathf.PI * _wheelRadius)) * _degreesPerRevolution;

            foreach (Transform wheel in _wheels)
            {
                Vector3 localPosition = transform.InverseTransformPoint(wheel.position);

                if (localPosition.x < 0)
                {
                    if (_carRigidbody.velocity.z > 0)
                    {
                        wheel.Rotate(Vector3.right * _wheelRotationSpeed * Time.deltaTime);
                    }
                    else
                    {
                        wheel.Rotate(Vector3.right * -_wheelRotationSpeed * Time.deltaTime);
                    }
                }
                else
                {
                    if (_carRigidbody.velocity.z > 0)
                    {
                        wheel.Rotate(Vector3.right * -_wheelRotationSpeed * Time.deltaTime);
                    }
                    else
                    {
                        wheel.Rotate(Vector3.right * _wheelRotationSpeed * Time.deltaTime);
                    }
                }
            }
        }
    }

    private void HandleSmokeEffects()
    {
        if (_isMoving)
        {
            if (_carSmokeEffect && !_carSmokeEffect.isPlaying)
            {
                _carSmokeEffect.Play();
            }
            if (_wheelSmokeEffect && Mathf.Abs(_steerInput) > _wheelSmokeSteerThreshold && !_wheelSmokeEffect.isPlaying)
            {
                _wheelSmokeEffect.Play();
            }
        }
        else
        {
            if (_carSmokeEffect && _carSmokeEffect.isPlaying)
            {
                _carSmokeEffect.Stop();
            }
            if (_wheelSmokeEffect && _wheelSmokeEffect.isPlaying)
            {
                _wheelSmokeEffect.Stop();
            }
        }
    }

    private void UpdateTrailRenderer()
    {
        _isDrifting = _isMoving && Mathf.Abs(_steerInput) > _wheelSmokeSteerThreshold;

        foreach (var trail in _regularTrails)
        {
            if (trail)
            {
                trail.emitting = _isMoving && !_isDrifting;
                UpdateTrailRendererAlpha(trail, _isMoving);
            }
        }

        foreach (var trailDrift in _driftTrails)
        {
            if (trailDrift)
            {
                trailDrift.emitting = _isDrifting;
                UpdateTrailRendererAlpha(trailDrift, _isDrifting);
            }
        }
    }

    private void UpdateTrailRendererAlpha(TrailRenderer trailRenderer, bool isEmitting)
    {
        Color color = trailRenderer.startColor;
        color.a = isEmitting ? 1f : 0f;

        trailRenderer.startColor = color;
        trailRenderer.endColor = color;
    }
}