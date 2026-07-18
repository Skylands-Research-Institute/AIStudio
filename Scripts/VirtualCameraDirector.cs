#if USE_CINEMACHINE
using Unity.Cinemachine;
#endif
using UnityEngine;

namespace DracarysInteractive.AIStudio
{
    public class VirtualCameraDirector : MonoBehaviour, IDialogueCameraDirector
    {
#if USE_CINEMACHINE
        public CinemachineVirtualCameraBase _virtualCamera;

        private CinemachineVirtualCameraBase virtualCamera
        {
            get
            {
                if (!_virtualCamera)
                {
                    _virtualCamera = FindFirstObjectByType<CinemachineVirtualCameraBase>();
                }

                return _virtualCamera;
            }
        }

        public void OnStartSpeaking(DialogueCharacter speaker)
        {
            virtualCamera.LookAt = speaker.lookAtTarget;
        }

        public void OnEndSpeaking(DialogueCharacter speaker)
        {
            virtualCamera.LookAt = null;
        }

        public void OnStartSpeechRecognition(DialogueCharacter speaker)
        {
            virtualCamera.LookAt = speaker.lookAtTarget;
        }

        public void OnSpeechRecognized(DialogueCharacter player)
        {
            virtualCamera.LookAt = null;
        }
#else
        public void OnEndSpeaking(DialogueCharacter character)
        {
            throw new System.NotImplementedException();
        }

        public void OnSpeechRecognized(DialogueCharacter character)
        {
            throw new System.NotImplementedException();
        }

        public void OnStartSpeaking(DialogueCharacter character)
        {
            throw new System.NotImplementedException();
        }

        public void OnStartSpeechRecognition(DialogueCharacter character)
        {
            throw new System.NotImplementedException();
        }
#endif
    }
}
