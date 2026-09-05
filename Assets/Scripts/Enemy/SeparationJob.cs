using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

// 각 적마다 반경 내 이웃을 전수 탐색해 분리 벡터를 계산.
// IJobParallelFor: index = 적 하나. positions는 모든 적이 공유(읽기 전용).
//
// 반경은 적마다 다르다. 전역 상수 하나를 쓰면 화면에서 2배 큰 탱커가 제 몸 절반이
// 상대 안에 들어와서야 밀리기 시작해 서로 겹쳐 보이고, 뒤에 있는 작은 적이 가려 안 보인다.
// 두 원의 접촉 판정과 같은 형태로, 이웃마다 "내 반경 + 상대 반경"을 임계값으로 쓴다.
[BurstCompile]
public struct SeparationJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float2> Positions;   // 모든 적 위치
    [ReadOnly] public NativeArray<int>    EntityIds;   // 겹침 시 결정적 오프셋용
    [ReadOnly] public NativeArray<float>  Radii;       // 적별 몸통 반경(월드 단위)
    [WriteOnly] public NativeArray<float2> Results;    // 적별 분리 벡터(가중치 미적용)

    public int Count;   // 활성 적 수 — 미사용 슬롯(유령 위치) 순회 방지

    public void Execute(int index)
    {
        float2 self   = Positions[index];
        int    selfId = EntityIds[index];
        float  selfR  = Radii[index];

        float2 separation = float2.zero;
        int count = Count;

        for (int i = 0; i < count; i++)
        {
            if (i == index) continue;

            float2 diff = self - Positions[i];
            float distSq = math.lengthsq(diff);

            // 두 몸통이 닿기 시작하는 거리. 큰 적끼리는 멀리서부터, 작은 적끼리는 가까이서 밀린다
            float radius = selfR + Radii[i];
            if (distSq >= radius * radius) continue;   // 반경 밖 — 조기 컷

            float dist = math.sqrt(distSq);
            if (dist < 0.0001f)
            {
                // 완전 겹침 — EntityId 기반 결정적 오프셋으로 NaN 방지(원본 로직 보존)
                diff = new float2((selfId & 1) == 0 ? 1f : -1f,
                                  (selfId & 2) == 0 ? 1f : -1f);
                dist = 1f;
            }

            float strength = 1f - (dist / radius);
            separation += (diff / dist) * strength;   // diff.normalized * strength
        }

        Results[index] = separation;
    }
}
