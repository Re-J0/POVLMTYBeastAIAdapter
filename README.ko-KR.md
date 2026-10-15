# POV LMTY BeastAI Adapter

**PathOfValheiman(POV)** 과 **LetMeTameYou(LMTY)** 를 함께 사용할 때 발생하는 길들이기/번식 호환 문제를 해결하는 Valheim용 BepInEx 어댑터입니다.

**현재 안정 버전: 0.3.1**

## 왜 필요한가?

테스트한 POV 4.13.10 + LMTY 0.1.0 조합에서 사슴/토끼 같은 일부 생물은 POV 적용 후 다음 형태가 됩니다.

```text
AI         = PassiveTree.BeastAI
MonsterAI  = true
AnimalAI   = false
Character  = true
Humanoid   = false
Tameable   = true
```

LMTY는 `MonsterAI`가 이미 존재하므로 원래 `AnimalAI` 변환 경로를 건너뛰지만, 이후 처리에서 `Humanoid`가 존재하는 구조를 기대합니다. POV가 변환한 사슴에는 `Humanoid`가 없어 `LetMeTameYou.TameManager.AddTame()`에서 `NullReferenceException`이 발생했습니다.

이 모드는 POV의 AI를 교체하지 않습니다. 대신 **실제로 문제가 확인된 컴포넌트 구조만 자동 감지**해서 LMTY 설정을 직접 적용합니다.

## 0.3.1의 핵심

프리팹 이름 하드코딩 목록이 없습니다.

다음 조건을 모두 만족할 때만 자동 복구합니다.

- `MonsterAI` 존재
- `Character` 존재
- `Humanoid` 없음
- `AnimalAI` 없음
- 런타임 AI가 `PassiveTree.BeastAI`, 다른 `PassiveTree.*`, 또는 PathOfValheiman 어셈블리 타입으로 높은 확률로 확인됨

정상적인 `MonsterAI + Humanoid` 생물은 원본 LMTY에 그대로 넘깁니다.

## 자동 복구 내용

- 필요한 경우 `Tameable` 추가
- LMTY의 `commandable`, `tamingTime`, `fedDuration` 적용
- 먹이 목록/검색 범위 적용
- POV BeastAI용 먹이 섭취 브리지
- 네이티브 타이머 정지 시에만 작동하는 적응형 길들이기 fallback
- `Procreation` 추가/설정
- `offspringPrefab` 지원 (`Seal -> Seal_Pup` 등)
- 그 외에는 LMTY `SpawnMini()` 사용

## 최종 테스트 결과

테스트 환경:

- Valheim 1.0.20
- BepInEx 5.4.23.5 / BepInExPack 5.4.2351
- PathOfValheiman 4.13.10
- LetMeTameYou 0.1.0
- BetterTames 0.9.0 사용 상태(선택 사항)

확인된 내용:

- 4성 Deer 길들이기 성공
- Deer 먹이 섭취/길들이기 진행/완료 성공
- Deer 임신/출산 성공
- Hare 구조 자동 감지 성공
- Seal 구조 자동 감지 및 `Seal_Pup` 연결 성공
- Hatchling/Drake는 어댑터 개입 없이 원본 LMTY 경로로 길들이기/번식/알 낳기 성공
- 3성 Hatchling 길들이기 성공
- 최종 검증 로그에서 `NullReferenceException`, 일반 `Exception`, `[Error` 없음

## 설치

Thunderstore/Gale/r2modman에서는 배포 패키지를 설치하면 됩니다.

수동 설치 시 필수 모드를 먼저 설치한 뒤:

```text
POVLMTYBeastAIAdapter.dll
```

을 다음 위치에 넣습니다.

```text
Valheim/BepInEx/plugins/POVLMTYBeastAIAdapter/
```

구버전 어댑터를 동시에 로드하지 마세요. 모든 버전은 동일한 BepInEx GUID를 사용합니다.

```text
com.rehyun.povlmtybeastaiadapter
```

## 제한 사항

LMTY의 특수 `egg:` 경로는 **POV의 문제가 되는 BeastAI 구조에 대해서는 아직 범용 재구현하지 않았습니다.**

현재 Hatchling/Drake는 `MonsterAI + Humanoid` 정상 구조이므로 이 제한에 해당하지 않고 원본 LMTY가 정상 처리합니다.

전용 서버 및 광범위한 멀티플레이 조합은 아직 충분히 검증하지 않았습니다.

더 자세한 원인 분석과 버전별 시행착오는 [`docs/DEVELOPMENT_HISTORY.md`](docs/DEVELOPMENT_HISTORY.md)를 참고하세요.
