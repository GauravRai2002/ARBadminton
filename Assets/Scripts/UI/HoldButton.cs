using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace ARBadmintonNet.UI
{
    /// <summary>
    /// Attach to a button to fire its action continuously while held down.
    /// Starts after initialDelay, then repeats every repeatInterval.
    /// </summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float initialDelay = 0.3f;
        [SerializeField] private float repeatInterval = 0.08f;
        
        private Action onHoldAction;
        private bool isHeld = false;
        private float holdTimer = 0f;
        private bool hasTriggeredInitial = false;
        
        public void SetAction(Action action)
        {
            onHoldAction = action;
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            isHeld = true;
            holdTimer = 0f;
            hasTriggeredInitial = false;
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            isHeld = false;
            holdTimer = 0f;
            hasTriggeredInitial = false;
        }
        
        private void Update()
        {
            if (!isHeld || onHoldAction == null) return;
            
            holdTimer += Time.deltaTime;
            
            if (!hasTriggeredInitial)
            {
                if (holdTimer >= initialDelay)
                {
                    hasTriggeredInitial = true;
                    holdTimer = 0f;
                    onHoldAction.Invoke();
                }
            }
            else
            {
                if (holdTimer >= repeatInterval)
                {
                    holdTimer = 0f;
                    onHoldAction.Invoke();
                }
            }
        }
        
        private void OnDisable()
        {
            isHeld = false;
            holdTimer = 0f;
            hasTriggeredInitial = false;
        }
    }
}
