using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RAXY.Notification
{
    public abstract class NotificationView : MonoBehaviour
    {
        const string EnterSlot = "NotificationEnter";
        const string ExitSlot = "NotificationExit";

        [SerializeField]
        AnimationClip enterClip;

        [SerializeField]
        AnimationClip exitClip;

        Action onDismiss;
        Coroutine exitRoutine;

        internal abstract void BindRequest(NotificationRequest request, Action dismiss);

        public virtual void Show()
        {
            if (exitRoutine != null)
            {
                StopCoroutine(exitRoutine);
                exitRoutine = null;
            }

            gameObject.SetActive(true);
            Play(enterClip, EnterSlot);
        }

        public virtual void Hide()
        {
            if (exitRoutine != null)
            {
                StopCoroutine(exitRoutine);
                exitRoutine = null;
            }

            gameObject.SetActive(false);
        }

        public void PlayExit(Action onFinished)
        {
            if (exitClip == null || !isActiveAndEnabled)
            {
                Hide();
                onFinished?.Invoke();
                return;
            }

            Play(exitClip, ExitSlot);
            exitRoutine = StartCoroutine(FinishExit(exitClip.length, onFinished));
        }

        public void Dismiss()
        {
            onDismiss?.Invoke();
        }

        protected void AcceptDismiss(Action dismiss)
        {
            onDismiss = dismiss;
        }

        protected TRequest Require<TRequest>(NotificationRequest request, Action dismiss)
            where TRequest : NotificationRequest
        {
            if (request is not TRequest typed)
            {
                throw new InvalidOperationException(
                    $"View {GetType().Name} expects {typeof(TRequest).Name} but received {request?.GetType().Name ?? "null"}.");
            }

            AcceptDismiss(dismiss);
            return typed;
        }

        void Play(AnimationClip clip, string slot)
        {
            if (clip == null)
                return;

            var animation = GetComponent<Animation>();
            if (animation == null)
                animation = gameObject.AddComponent<Animation>();

            animation.playAutomatically = false;
            if (animation.GetClip(slot) != null)
                animation.RemoveClip(slot);

            animation.AddClip(clip, slot);
            animation.Play(slot);
        }

        IEnumerator FinishExit(float length, Action onFinished)
        {
            if (length > 0f)
                yield return new WaitForSeconds(length);

            exitRoutine = null;
            Hide();
            onFinished?.Invoke();
        }
    }

    public abstract class NotificationView<TRequest> : NotificationView
        where TRequest : NotificationRequest
    {
        internal override void BindRequest(NotificationRequest request, Action dismiss)
        {
            Bind(Require<TRequest>(request, dismiss));
        }

        protected abstract void Bind(TRequest request);
    }

    public abstract class FullscreenNotificationView : NotificationView
    {
        [SerializeField]
        Button closeButton;

        protected virtual void Awake()
        {
            if (closeButton == null)
            {
                Debug.LogError($"{GetType().Name} has no close button.", this);
                return;
            }

            closeButton.onClick.AddListener(Dismiss);
        }

        protected virtual void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Dismiss);
        }
    }

    public abstract class FullscreenNotificationView<TRequest> : FullscreenNotificationView
        where TRequest : NotificationRequest
    {
        internal override void BindRequest(NotificationRequest request, Action dismiss)
        {
            Bind(Require<TRequest>(request, dismiss));
        }

        protected abstract void Bind(TRequest request);
    }
}
