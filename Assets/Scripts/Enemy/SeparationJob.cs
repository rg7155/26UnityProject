using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

// 각 적마다 separationRadius 내 이웃을 전수 탐색해 분리 벡터를 계산.
// IJobParallelFor: index = 적 하나. positions는 모든 적이 공유(읽기 전용).
[BurstCompile]
public struct SeparationJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float2> Positions;   // 모든 적 위치
    [ReadOnly] public NativeArray<int>    EntityIds;   // 겹침 시 결정적 오프셋용
    [WriteOnly] public NativeArray<float2> Results;    // 적별 분리 벡터(가중치 미적용)

    public float SeparationRadius;

    public void Execute(int index)
    {
        float2 self     = Positions[index];
        int    selfId   = EntityIds[index];
        float  radius   = SeparationRadius;
        float  radiusSq = radius * radius;

        float2 separation = float2.zero;
        int count = Positions.Length;

        for (int i = 0; i < count; i++)
        {
            if (i == index) continue;

            float2 diff = self - Positions[i];
            float distSq = math.lengthsq(diff);
            if (distSq >= radiusSq) continue;   // 반경 밖 — 조기 컷

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
