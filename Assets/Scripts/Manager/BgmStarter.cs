using UnityEngine;
using UnityEngine.InputSystem;

// @SoundRoot에 부착. 첫 사용자 입력 전까지 BGM 재생을 미룬다.
// WebGL은 사용자 제스처 전까지 AudioContext가 잠겨 있고, 잠긴 채 Play하면 재생 헤드만 흘러가
// 도입부가 통째로 유실된다. 게다가 isPlaying은 true라 나중에 감지해서 되돌릴 수도 없다.
// 플랫폼 분기 없이 단일 경로로 두어 에디터에서 검증한 동작이 곧 WebGL 동작이 되게 한다.
public class BgmStarter : MonoBehaviour
{
    void Update()
    {
        if (HasUserInput() == false)
            return;

        Managers.Sound.PlayBgm();
        Destroy(this);   // @SoundRoot는 DontDestroyOnLoad 사운드 루트 — 컴포넌트만 제거
    }

    bool HasUserInput()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        return false;
    }
}
