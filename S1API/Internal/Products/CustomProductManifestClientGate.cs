using System;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Models the client-side ordering gate independently of FishNet.
    /// </summary>
    internal sealed class CustomProductManifestClientGate
    {
        private Action? _deferredPlayerDataRequest;

        internal bool IsWaiting { get; private set; }

        internal void Begin(bool requiresValidation)
        {
            IsWaiting = requiresValidation;
            _deferredPlayerDataRequest = null;
        }

        internal bool AuthorizePlayerDataRequest(Action request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!IsWaiting)
                return true;

            if (_deferredPlayerDataRequest == null)
                _deferredPlayerDataRequest = request;
            return false;
        }

        internal CustomProductManifestAcceptance Accept(
            string hostCompatibilityHash,
            string localCompatibilityHash,
            out Action? deferredPlayerDataRequest)
        {
            deferredPlayerDataRequest = null;
            if (!string.Equals(
                    hostCompatibilityHash,
                    localCompatibilityHash,
                    StringComparison.Ordinal))
            {
                return CustomProductManifestAcceptance.Incompatible;
            }

            if (!IsWaiting)
                return CustomProductManifestAcceptance.AlreadyAccepted;

            IsWaiting = false;
            deferredPlayerDataRequest = _deferredPlayerDataRequest;
            _deferredPlayerDataRequest = null;
            return CustomProductManifestAcceptance.Accepted;
        }

        internal void End()
        {
            IsWaiting = false;
            _deferredPlayerDataRequest = null;
        }
    }

    internal enum CustomProductManifestAcceptance
    {
        Accepted,
        AlreadyAccepted,
        Incompatible
    }
}
