#if (MONOMELON || MONOBEPINEX)
using S1Recycler = ScheduleOne.ObjectScripts.Recycler;
#else
using S1Recycler = Il2CppScheduleOne.ObjectScripts.Recycler;
#endif

using UnityEngine;
using UnityEngine.Events;

namespace S1API.Trash
{
    /// <summary>
    /// Provides read-only access to a native world recycler.
    /// </summary>
    /// <remarks>
    /// Use this wrapper while its native object exists in the loaded scene. Obtain a new
    /// snapshot from <see cref="RecyclerRegistry.GetAll"/> after a scene change.
    /// </remarks>
    public sealed class Recycler
    {
        private readonly UnityEvent? _onStart;
        private readonly UnityEvent? _onStop;
        private readonly UnityAction _startListener;
        private readonly UnityAction _stopListener;
        private bool _listening;

        internal readonly S1Recycler S1Recycler;

        internal Recycler(S1Recycler recycler)
        {
            S1Recycler = recycler;
            _onStart = recycler.onStart;
            _onStop = recycler.onStop;
#if (MONOMELON || MONOBEPINEX)
            _startListener = new UnityAction(NotifyProcessingStarted);
            _stopListener = new UnityAction(NotifyProcessingStopped);
#else
            // Retain the converted delegates so removal uses the same native targets.
            _startListener = (UnityAction)NotifyProcessingStarted;
            _stopListener = (UnityAction)NotifyProcessingStopped;
#endif

            try
            {
                if (_onStart != null)
                    _onStart.AddListener(_startListener);
                if (_onStop != null)
                    _onStop.AddListener(_stopListener);
                _listening = true;
            }
            catch
            {
                Cleanup();
                throw;
            }
        }

        /// <summary>
        /// Gets the current native state. Processing may continue to be reported after the stop signal.
        /// </summary>
        public RecyclerState State =>
            (RecyclerState)(int)S1Recycler.State;

        /// <summary>Gets whether the native hatch is open.</summary>
        public bool IsHatchOpen =>
            S1Recycler.IsHatchOpen;

        /// <summary>Gets the recycler's world position.</summary>
        public Vector3 Position =>
            S1Recycler.transform.position;

        /// <summary>Gets the game object that owns the native recycler.</summary>
        public GameObject GameObject =>
            S1Recycler.gameObject;

        internal void Cleanup()
        {
            _listening = false;
            if (_onStart != null)
                _onStart.RemoveListener(_startListener);
            if (_onStop != null)
                _onStop.RemoveListener(_stopListener);
        }

        private void NotifyProcessingStarted()
        {
            if (_listening)
                RecyclerRegistry.NotifyProcessingStarted(this);
        }

        private void NotifyProcessingStopped()
        {
            if (_listening)
                RecyclerRegistry.NotifyProcessingStopped(this);
        }
    }
}
