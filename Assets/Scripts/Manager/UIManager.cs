using System;
using System.Collections.Generic;
using UnityEngine;

// UI 레이어 단일 출처. 값 = Canvas.sortingOrder (간격 100은 후속 삽입 여유).
// Cutscene 은 최상단이다 — 오프닝 오버레이가 타이틀 UI 를 완전히 덮어야 한다.
public enum UILayer { Background = 0, Hud = 100, Modal = 300, Pause = 400, Cutscene = 500 }

// 1주차: 최소 스텁. 추후 팝업/씬 UI 시스템 구현 예정.
public class UIManager
{
    public void Init() { }
    public void Clear() { }
}
