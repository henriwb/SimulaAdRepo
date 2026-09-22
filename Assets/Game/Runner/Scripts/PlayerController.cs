using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// A class used to control a player in a Runner
    /// game. Includes logic for player movement as well as 
    /// other gameplay logic.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        /// <summary> Returns the PlayerController. </summary>
        public static PlayerController Instance => s_Instance;
        private static PlayerController s_Instance;

        [SerializeField]
        private Animator m_Animator;

        [SerializeField]
        SkinnedMeshRenderer m_SkinnedMeshRenderer;
        // THIS IS CODE EXAMPLE OF LunaPlaygroundFields usage
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        [ LunaPlaygroundField("Player Speed", 1, "Gameplay Settings")]
#endif
        [SerializeField]
        private PlayerSpeedPreset m_PlayerSpeed = PlayerSpeedPreset.Custom;

#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        [ LunaPlaygroundFieldStep( 1 ) ]
        [LunaPlaygroundField("Custom Player Speed", 0, "Gameplay Settings")]
#endif
        [ Range( 1, 100 ) ]
        [SerializeField]
        private float m_CustomPlayerSpeed = 10.0f;

        [SerializeField]
        private float m_AccelerationSpeed = 10.0f;

        [SerializeField]
        private float m_DecelerationSpeed = 20.0f;

        [SerializeField]
        private float m_HorizontalSpeedFactor = 0.5f;

        [SerializeField]
        private float m_ScaleVelocity = 2.0f;

        [SerializeField]
        private bool m_AutoMoveForward = true;
        
        [SerializeField]
        private float m_MaxXPosition = 1f;

        [SerializeField] private bool m_KeepMovingOnceStarted = true;

        private bool m_RecievedInput;

        Vector3 m_LastPosition;
        private float m_StartHeight;

        private const float k_MinimumScale = 0.1f;
        private static readonly string s_Speed = "Speed";
        private static readonly string s_win = "Win";

        enum PlayerSpeedPreset
        {
            Slow,
            Medium,
            Fast,
            Custom
        }

        private Transform m_Transform;
        private Vector3 m_StartPosition;
        private bool m_HasInput;
        private float m_XPos;
        private float m_ZPos;
        private float m_TargetPosition;
        private float m_Speed;
        private float m_TargetSpeed;
        private Vector3 m_Scale;
        private Vector3 m_TargetScale;

        /// <summary> The player's default local scale. </summary>
        public Vector3 DefaultScale { get; private set; }

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;

            Initialize();
        }

        /// <summary>
        /// Set up all necessary values for the PlayerController.
        /// </summary>
        public void Initialize()
        {
            m_Transform = transform;
            m_StartPosition = m_Transform.position;
            DefaultScale = m_Transform.localScale;
            m_Scale = DefaultScale;
            m_TargetScale = m_Scale;

            m_StartHeight = m_SkinnedMeshRenderer != null ? m_SkinnedMeshRenderer.bounds.size.y : 1.0f;

            ResetSpeed();
        }

        /// <summary>
        /// Returns the current default speed based on the currently
        /// selected PlayerSpeed preset.
        /// </summary>
        public float GetDefaultSpeed()
        {
            switch (m_PlayerSpeed)
            {
                case PlayerSpeedPreset.Slow:
                    return 5.0f;

                case PlayerSpeedPreset.Medium:
                    return 10.0f;

                case PlayerSpeedPreset.Fast:
                    return 20.0f;
            }

            return m_CustomPlayerSpeed;
        }

        /// <summary>
        /// Adjust the player's current speed
        /// </summary>
        public void AdjustSpeed(float speed)
        {
            m_TargetSpeed += speed;
            m_TargetSpeed = Mathf.Max(0.0f, m_TargetSpeed);
        }

        /// <summary>
        /// Reset the player's current speed to their default speed
        /// </summary>
        public void ResetSpeed()
        {
            m_Speed = 0.0f;
            m_TargetSpeed = GetDefaultSpeed();
        }

        /// <summary>
        /// Adjust the player's current scale
        /// </summary>
        public void AdjustScale(float scale)
        {
            m_TargetScale += Vector3.one * scale;
            m_TargetScale = Vector3.Max(m_TargetScale, Vector3.one * k_MinimumScale);
            AudioManager.Instance.PlayEffect(SoundID.ButtonSound);
        }

        /// <summary>
        /// Reset the player's current speed to their default speed
        /// </summary>
        public void ResetScale()
        {
            m_Scale = DefaultScale;
            m_TargetScale = DefaultScale;
        }

        /// <summary>
        /// Returns the player's transform component
        /// </summary>
        public Vector3 GetPlayerTop()
        {
            return m_Transform.position + Vector3.up * (m_StartHeight * m_Scale.y - m_StartHeight);
        }

        /// <summary>
        /// Sets the target X position of the player
        /// </summary>
        public void SetDeltaPosition(float normalizedDeltaPosition)
        {
            float fullWidth = m_MaxXPosition * 2.0f;
            m_TargetPosition = m_TargetPosition + fullWidth * normalizedDeltaPosition;
            m_TargetPosition = Mathf.Clamp(m_TargetPosition, -m_MaxXPosition, m_MaxXPosition);
            m_HasInput = true;
        }

        /// <summary>
        /// Stops player movement
        /// </summary>
        public void CancelMovement()
        {
            m_HasInput = false;
        }

        public void PlayerWon()
        {
            CancelMovement();
            m_Animator.SetTrigger(s_win);
            this.enabled = false;
        }
        
        /// <summary>
        /// Returns player to their starting position
        /// </summary>
        public void ResetPlayer()
        {
            m_Transform.position = m_StartPosition;
            m_XPos = 0.0f;
            m_ZPos = m_StartPosition.z;
            m_TargetPosition = 0.0f;

            m_LastPosition = m_Transform.position;

            m_HasInput = false;

            ResetSpeed();
            ResetScale();
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;

            // Update Scale

            if (!Approximately(m_Transform.localScale, m_TargetScale))
            {
                m_Scale = Vector3.Lerp(m_Scale, m_TargetScale, deltaTime * m_ScaleVelocity);
                m_Transform.localScale = m_Scale;
            }

            // Update Speed

            if (!m_AutoMoveForward && !m_HasInput && !(m_KeepMovingOnceStarted && m_RecievedInput))
            {
                Decelerate(deltaTime, 0.0f);
            }
            else if (m_TargetSpeed < m_Speed)
            {
                Decelerate(deltaTime, m_TargetSpeed);
            }
            else if (m_TargetSpeed > m_Speed)
            {
                Accelerate(deltaTime, m_TargetSpeed);
            }

            var speed = m_Speed * deltaTime;

            // Update position

            m_ZPos += speed;

            if (m_HasInput)
            {
                var horizontalSpeed = speed * m_HorizontalSpeedFactor;

                var newPositionTarget = Mathf.Lerp(m_XPos, m_TargetPosition, horizontalSpeed);
                var newPositionDifference = newPositionTarget - m_XPos;

                newPositionDifference = Mathf.Clamp(newPositionDifference, -horizontalSpeed, horizontalSpeed);

                m_XPos += newPositionDifference;

                m_RecievedInput = true;
            }

            m_Transform.position = new Vector3(m_XPos, m_Transform.position.y, m_ZPos);

            if (m_Animator != null && deltaTime > 0.0f)
            {
                var distanceTravelledSinceLastFrame = (m_Transform.position - m_LastPosition).magnitude;
                var distancePerSecond = distanceTravelledSinceLastFrame / deltaTime;

                m_Animator.SetFloat(s_Speed, distancePerSecond);
            }

            if (m_Transform.position != m_LastPosition)
            {
                m_Transform.forward = Vector3.Lerp(m_Transform.forward, (m_Transform.position - m_LastPosition).normalized, speed);
            }

            m_LastPosition = m_Transform.position;

            if (Input.GetMouseButtonUp(0))
            {
                m_TargetPosition = transform.localPosition.x;
            }
        }

        void Accelerate(float deltaTime, float targetSpeed)
        {
            m_Speed += deltaTime * m_AccelerationSpeed;
            m_Speed = Mathf.Min(m_Speed, targetSpeed);
        }

        void Decelerate(float deltaTime, float targetSpeed)
        {
            m_Speed -= deltaTime * m_DecelerationSpeed;
            m_Speed = Mathf.Max(m_Speed, targetSpeed);
        }

        bool Approximately(Vector3 a, Vector3 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
        }

    }
}