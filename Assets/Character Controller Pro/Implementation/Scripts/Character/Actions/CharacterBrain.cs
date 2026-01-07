
using UnityEngine;
using Lightbug.Utilities;
using Lightbug.CharacterControllerPro.Demo;

namespace Lightbug.CharacterControllerPro.Implementation
{
    [AddComponentMenu("Character Controller Pro/Implementation/Character/Character Brain")]
    [DefaultExecutionOrder(int.MinValue)]
    public class CharacterBrain : MonoBehaviour
    {
        public UpdateModeType UpdateMode = UpdateModeType.FixedUpdate;

        [BooleanButton("Brain type", "Player", "AI", true)]
        [SerializeField]
        bool isAI = false;

        [Condition("isAI", ConditionAttribute.ConditionType.IsFalse)]
        [Expand]
        [SerializeField]
        public InputHandlerSettings inputHandlerSettings = new InputHandlerSettings();

        [Condition("isAI", ConditionAttribute.ConditionType.IsTrue)]
        [SerializeField]
        CharacterAIBehaviour aiBehaviour = null;

        public CharacterActions characterActions;

        public NormalMovement normalMovement;

        public enum UpdateModeType { FixedUpdate, Update }

        CharacterAIBehaviour currentAIBehaviour = null;
        bool firstUpdateFlag = false;

        public bool IsAI => isAI;
        public void SetAction(CharacterActions characterActions) => this.characterActions = characterActions;

        public void SetBrainType(bool isAI)
        {
            characterActions.Reset();
            if (isAI)
                SetAIBehaviour(aiBehaviour);
            this.isAI = isAI;
        }

        public void SetInputHandler(InputHandler inputHandler)
        {
            if (inputHandler == null)
                return;
            inputHandlerSettings.InputHandler = inputHandler;
            characterActions.Reset();
        }

        public void SetAIBehaviour(CharacterAIBehaviour aiBehaviour)
        {
            if (aiBehaviour == null)
                return;
            currentAIBehaviour?.ExitBehaviour(Time.deltaTime);
            characterActions.Reset();
            currentAIBehaviour = aiBehaviour;
            currentAIBehaviour.EnterBehaviour(Time.deltaTime);
        }

        public void UpdateBrainValues(float dt)
        {
            if (Time.timeScale == 0)
                return;

            if (IsAI)
                UpdateAIBrainValues(dt);
            else
                UpdateHumanBrainValues(dt);
        }

        void UpdateHumanBrainValues(float dt)
        {
            characterActions.Update(dt);
        }

        void UpdateAIBrainValues(float dt)
        {
            currentAIBehaviour?.UpdateBehaviour(dt);
            characterActions.Update(dt);
        }

        #region Unity's messages

        protected virtual void Awake()
        {
            characterActions = new CharacterActions();
            characterActions.InitializeActions();
            inputHandlerSettings.Initialize(gameObject);
        }

        protected virtual void OnEnable()
        {
            characterActions.InitializeActions();
            characterActions.Reset();
        }

        protected virtual void OnDisable()
        {
            characterActions.Reset();
        }

        void Start()
        {
            SetBrainType(isAI);
        }

        protected virtual void FixedUpdate()
        {
            firstUpdateFlag = true;
            if (UpdateMode == UpdateModeType.FixedUpdate)
            {
                UpdateBrainValues(0f);
            }
        }

        protected virtual void Update()
        {
            float dt = Time.deltaTime;

            if (UpdateMode == UpdateModeType.FixedUpdate)
            {
                if (firstUpdateFlag)
                {
                    firstUpdateFlag = false;
                    characterActions.Reset();
                }
            }
            else
            {
                characterActions.Reset();
            }


            UpdateBrainValues(dt);
        }


        #endregion
    }
}
