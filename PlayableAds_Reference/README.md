# Loop Sort - phân tích gameplay cho playable ads

## Mục đích của folder này

Folder này nằm ngoài `Assets` để Unity của project gốc không compile thêm một bản script trùng class.

- `OriginalScripts/`: 121 script quan trọng, được copy nguyên trạng từ source hiện tại.
- `OptionalOriginalScripts/`: 26 script booster, vibration và UI gameplay cũ; chỉ dùng để tham khảo.
- `ReferenceData/LevelData/`: bốn level `Level_PLA_1` đến `Level_PLA_4` đã có sẵn trong project.

Đây là gói tham chiếu để port gameplay, chưa phải một package có thể thả thẳng vào project Unity mới và compile. Một số script core của game gốc gọi trực tiếp hệ thống app như `DataManager`, `HeartSystem`, `LayerManager`, tracking và booster. Các điểm cần cắt được ghi rõ bên dưới.

## Gameplay cốt lõi

Đây là game sort theo vòng tuần hoàn:

1. Mỗi `Carrier` là một khay có tối đa bốn `Block`.
2. Mỗi block chứa nhiều cube/slime cùng màu. Số cube trong một block được tính từ `CapacityManager` và grid của level.
3. Người chơi bấm vào carrier. Carrier mở block hợp lệ ở trên cùng, hoặc một chuỗi block cùng màu liên tiếp.
4. Cube bay từ block ra conveyor rồi chạy dọc spline bằng `Rigidbody` và lực bám đường.
5. Khi cube đi ngang điểm pickup của carrier, hệ thống tự chọn carrier nhận phù hợp:
   - ưu tiên special receiver đúng màu;
   - ưu tiên carrier đã có cùng màu hơn carrier rỗng;
   - ưu tiên one-way carrier phù hợp;
   - không cho cube quay lại carrier nguồn trước khi đi đủ một vòng;
   - mỗi carrier chỉ nhận một màu trong cùng một đợt bay.
6. Khi một carrier có đủ bốn block đầy cùng màu, carrier đó hoàn thành.
7. Thắng khi tất cả màu bắt buộc đã hoàn thành và conveyor không còn cube.
8. Thua khi conveyor/capacity đầy và không còn nước đi hợp lệ, hoặc deadlock được xác nhận sau khi trạng thái đã ổn định.

Luồng runtime chính:

```text
InputController
  -> Carrier.OnObjectClicked
  -> CarrierUnloadPort
  -> CarrierUnloadPayloadCollector
  -> ConveyorUnloadHandler
  -> ConveyorDeliverySystem.CompleteUnload
  -> CubeMovement chạy trên spline
  -> ForwardBestSlotPickupRule
  -> CarrierReceivePort
  -> Carrier hoàn thành màu
  -> ConveyorWinDetector / ConveyorLoseDetector
  -> GameConditionManager
```

## Trình tự khởi tạo level

`LevelManager.LoadLevel()` đang làm các bước sau:

1. Hủy lượt load cũ, khóa input và xóa cube của level trước.
2. Lấy `LevelData`.
3. Reset trạng thái thắng/thua, pre-lose và custom time scale.
4. Áp dụng orthographic size cho camera.
5. `CapacityManager.Init(levelData)`.
6. `CarrierSystem.InitCarrier(levelData)`.
7. Khởi tạo swapping block.
8. `ConveyorManager.InitConveyorAsync(levelData.SplineLayout)`.
9. Chạy animation reveal conveyor, container và carrier.
10. Khởi tạo booster và block-link visual.
11. Phát `OnLevelLoaded`, mở input.

Trong playable ads nên giữ thứ tự này, nhưng thay bước lấy level bằng một `LevelData` cố định được gán trong Inspector.

## Những script quan trọng nhất

### Dữ liệu và bootstrap

| Script | Vai trò | Xử lý khi port |
| --- | --- | --- |
| `LevelData.cs` | Schema toàn bộ level: capacity, carrier, block, mechanic, spline | Giữ |
| `LevelManager.cs` | Điều phối init/reset level | Viết bản lean, bỏ app shell |
| `ConfigManager.cs` | Cấp prefab, màu và movement config | Giữ hoặc gom thành một config playable |
| `LevelConfigSO.cs` | Danh sách/loop level game thường | Không cần nếu playable chỉ có một level |
| `MonoSingleton.cs` | Singleton cho các manager | Có thể giữ |
| `GameEventBus.cs` | Event giữa gameplay, UI và manager | Viết bản lean; file gốc chứa nhiều type shop/IAP |

### Input, carrier và block

| Script/nhóm | Vai trò |
| --- | --- |
| `InputController.cs` | Raycast click/touch, gọi `IClickableObject` |
| `CarrierBase.cs`, `Carrier.cs` | API và state chính của khay |
| `CarrierSpawner.cs`, `CarrierSystem.cs` | Spawn carrier từ `LevelData`, đăng ký pickup |
| `CarrierRuntimeState.cs` | Idle, Unloading, Completed, Finished |
| `CarrierBlockController.cs` | Chọn block trên cùng, chuỗi cùng màu, vị trí nhận |
| `CarrierUnloadPort.cs` | Kiểm tra mechanic/capacity rồi tạo request unload |
| `CarrierReceivePort.cs` | Reserve cell, nhận cube, phát hiện carrier hoàn thành |
| `Block.cs` | Màu, số cube, hidden/key/link/swap mechanic |
| `BlockOpenHandler.cs`, `BlockReceiveHandler.cs` | Logic mở và nhận block tách khỏi visual |

### Conveyor và cube

| Script/nhóm | Vai trò |
| --- | --- |
| `ConveyorManager.cs` | Dựng spline, mesh, portal và animation reveal |
| `ConveyorDeliverySystem.cs` | Trung tâm điều phối cube trên conveyor |
| `ConveyorUnloadHandler.cs` | Animation cube từ carrier ra conveyor |
| `ConveyorPickupHandler.cs` | Animation cube từ conveyor vào carrier |
| `ForwardBestSlotPickupRule.cs` | Luật chọn carrier nhận cube |
| `CubeMovement.cs` | Di chuyển vật lý dọc spline |
| `ConveyorCubeSpeedController.cs` | Boost ở góc, portal và vùng spawn |
| `ConveyorSpawnPointCalculator.cs` | Rải cube lên bề rộng conveyor |
| `PoolManagerNew.cs` | Pool carrier, cube, anim cube, portal và effect |

### Win, lose và capacity

| Script | Vai trò |
| --- | --- |
| `CapacityManager.cs` | Đếm current + pending cube và giới hạn conveyor |
| `ConveyorWinDetector.cs` | Theo dõi các màu phải hoàn thành |
| `ConveyorLoseDetector.cs` | Lose khi capacity đầy và không carrier nào nhận được |
| `DeadlockDetector.cs` | Kiểm tra kẹt nâng cao khi còn 1-2 block capacity |
| `GameConditionManager.cs` | Chốt win/lose, slow motion, camera shake, mở UI kết quả |
| `CustomTimeScaleGroup.cs` | Time scale riêng cho cube/animation, không đổi `Time.timeScale` toàn cục |

### Sound

Các file gốc được giữ trong `OriginalScripts`:

- `SoundManager.cs`
- `SoundData.cs`
- `SoundDataSO.cs`

Sound gameplay thực sự cần cho playable:

- `sfx_touch_box`
- `sfx_merge`
- `sfx_merge_loop`
- `sfx_squash_end`
- `sfx_endgame_lose`
- một victory SFX/BGM nếu creative cần

`SoundManager` gốc không nên mang nguyên sang playable vì phụ thuộc `DataManager`, `ConfigHolder`, booster tutorial, remote config và setting. Nên viết manager nhỏ gồm 2-3 `AudioSource`, một bảng clip và các API `PlayOneShot`, `PlayLoop`, `StopLoop`.

## Mechanic có trong source

Block mechanic:

- `HiddenBlock`: block sau chỉ lộ khi block trước được giải phóng.
- `KeyUnlockContainer`: block mang chìa khóa mở container.
- `BlockLink`: nhiều block/carrier phải unload như một nhóm.
- `SwappingBlock`: cặp block đổi màu sau thao tác.

Carrier mechanic:

- `HiddenByColor`: carrier ẩn đến khi một màu hoàn thành.
- `OneWay`: giới hạn unload/receive.
- `SpecialColorReceiver`: carrier chỉ nhận một màu mục tiêu.
- `Spawner`: carrier chỉ đẩy lần lượt queue block ra, không nhận cube.

Với playable ads đầu tiên, chỉ nên dùng `SpecialColorReceiver`. Đây cũng là cách bốn level PLA hiện có được thiết kế.

## Bốn level PLA có sẵn

| Level | Carrier | Màu | Capacity | Camera | Spline |
| --- | ---: | --- | ---: | ---: | --- |
| `Level_PLA_1` | 5 | Red, Pink, Yellow, Orange, Cyan | 8 | 25 | mở, 8 node |
| `Level_PLA_2` | 6 | Red, Pink, Yellow, Purple, LimeGreen, Cyan | 8 | 25 | mở, 7 node |
| `Level_PLA_3` | 5 | Red, Pink, Yellow, Orange, Cyan | 8 | 22 | mở, 12 node |
| `Level_PLA_4` | 6 | Red, Pink, Yellow, Purple, LimeGreen, Cyan | 8 | 22 | mở, 8 node |

Đặc điểm chung:

- Không có `BoosterCarriers`.
- Không có `Containers`.
- Mọi carrier dùng `SpecialColorReceiver`.
- Các level có ID 261-264 nhưng hiện không được thêm vào `LevelConfigSO` của game thường.

Khuyến nghị creative đầu tiên: dùng `Level_PLA_1` hoặc `Level_PLA_3` vì chỉ có năm carrier/màu, dễ đọc trên màn hình nhỏ và thời gian hoàn thành ngắn hơn.

## Setup scene gốc cần mô phỏng

`S_GamePlay.unity` có các object quan trọng:

- `LevelManager`
- `ConfigManager`
- `CapacityManager`
- `CarrierSystem` + `CarrierSpawner`
- `ConveyorManager`
- `ConveyorDeliverySystem`
- `ConveyorMeshBuilder`
- `ConveyorCornerDetector`
- `InputManager`
- `GameConditionManager`
- `PoolManagerNew`
- `CustomTimeScaleGroup`
- `CameraManager`
- `Main Camera` và `HighlightCamera`
- `BlockLinkSystem`, `BlockLinkVisualManager`, `SwappingBlockManager`
- `ComplimentManager`

Trong playable dùng level PLA, có thể bỏ `BlockLinkSystem`, `BlockLinkVisualManager`, `SwappingBlockManager`, `ComplimentManager`, `HighlightCamera` và toàn bộ booster nếu đã xóa/no-op các lời gọi liên quan.

Các giá trị serialized đáng chú ý trong scene gốc:

- `InputController.clickableLayerMask = 119`.
- `ConveyorDeliverySystem.spawnInterval = 0.02`.
- `pickupColliderWidth = 2`, `pickupColliderHeight = 0.4`, `pickupColliderDepth = 0.3`.
- `pickupThreshold = 0.05`.
- Level entry: reveal delay `0.1s`, reveal duration `1s`, carrier scale duration `0.3s`, stagger `0.1s`.
- Game condition: win delay `1s`, lose delay `3s`, lose shake `0.3s / 0.3`, win-guarantee speed `2.5x`.

Các config đang được scene gốc gán trực tiếp:

- `LevelConfigSO.asset`
- `LevelEntryAnimConfigSO.asset`
- `ColorConfigSO.asset`, `CubeColorConfigSO.asset`, `SpecialColorConfigSO.asset`
- `CubeConfigSO.asset`, `CubeMovementConfigSO.asset`
- `CarrierConfigSO.asset`
- `CatColorConfigSO.asset`, `StylizedColorConfigSO.asset`, `RemainingColorConfigSO.asset`
- `AnimBlockConfig.asset`
- `ConveyorSpawnPointConfigSO.asset`, `ConveyorSpeedBoostConfigSO.asset`, `ConveyorCornerDetectorConfigSO.asset`
- `GameConditionConfigSO.asset`

Các prefab/asset bắt buộc ngoài script:

- Carrier prefab và block prefab.
- Cube prefab, anim-cube prefab.
- Conveyor segment/mesh material và portal nếu dùng spline mở.
- Color config, cube movement config, carrier config.
- Conveyor spawn/speed/corner config.
- Level entry animation config.
- Material, mesh, texture và shader mà các prefab trên tham chiếu.
- Audio clip tối thiểu nếu giữ sound.

## Phụ thuộc package

Core hiện tại dùng:

- Unity Splines 2.8.4.
- Unity Mathematics.
- LitMotion.
- UniTask.
- TextMeshPro, chủ yếu cho `Spawner`.
- MemoryPack, chỉ để serialize level data/remote level.
- Alchemy Inspector, chủ yếu là attribute editor.
- Vibration utility.

Đối với playable:

- Nên giữ Unity Splines.
- Có thể giữ LitMotion và UniTask nếu pipeline playable hỗ trợ; nếu giới hạn bundle nghiêm ngặt, thay bằng coroutine/tween nhỏ.
- Có thể bỏ MemoryPack nếu dùng ScriptableObject level cố định.
- Có thể bỏ Alchemy bằng cách xóa attribute.
- Bỏ vibration hoặc thay bằng no-op.
- Không cần Addressables cho một level cố định.

## Những phần phải bỏ

Không cần mang sang playable:

- Loading scene, main menu và game-state app shell.
- Account/profile/avatar/frame.
- Setting/language/localization đầy đủ.
- Shop, currency, IAP, offer pack, daily reward.
- Heart/lives.
- Ads mediation trong game.
- Firebase, Adjust, AppMetrica, AppsFlyer, Facebook SDK.
- Notification, remote config, download manager.
- Level remote download/upload, zip, manifest, S3.
- Save/load player progression.
- Rating, feedback, internet/update popup.
- End-game reward/gold flow.

Playable vẫn cần SDK CTA/click/install của nhà cung cấp playable, nhưng phần đó nên là adapter riêng của project mới, không dùng ad manager trong game gốc.

## Các coupling phải cắt trước khi compile project mới

1. `LevelManager`
   - Bỏ `DataManager`, `HeartSystem`, `UserBehaviorTracker`, `CheckShowNewFeature`, `GameStateManager`, `SceneLoader`, `UIGeneratorLevelProcess`.
   - Gán trực tiếp một `LevelData playableLevel`.
   - Không loop level, không remote load.

2. `GameConditionManager`
   - Bỏ tracking, pre-lose purchase/booster popup và `LayerManager`.
   - Phát event `OnPlayableWin/OnPlayableLose` cho HUD/CTA.

3. `InputController`
   - Bỏ `HeartSystem`, `GameStateManager`, `LayerManager`.
   - Chỉ cần cờ `inputEnabled` và chặn khi CTA/result đang mở.

4. `CustomTimeScaleGroup`
   - Bỏ kiểm tra main menu, popup và claw booster.

5. `GameEventBus`
   - Không copy nguyên file vào project lean vì file gốc khai báo event cho IAP/shop/profile.
   - Chỉ giữ event gameplay được dùng trong các script đã port.

6. `SoundManager`
   - Viết bản lean như mô tả ở phần Sound.

7. Undo/booster
   - `CarrierUnloadPort`, `ConveyorUnloadHandler`, `ConveyorDeliverySystem` gọi trực tiếp `BoosterUndoSystem`.
   - Nếu playable không có undo, xóa các lời gọi record/notify hoặc tạo facade no-op nhỏ.
   - `Carrier` và `InputController` cũng có nhánh `BoosterSystem`; xóa các nhánh này.

8. Vibration
   - `ConveyorPickupHandler` và `ConveyorUnloadHandler` gọi `VibrationManager`; xóa hoặc no-op.

## Cấu trúc project playable được đề xuất

```text
Assets/
  Playable/
    Art/
    Audio/
    Config/
    Level/
      Level_PLA_1.asset
    Prefabs/
    Scenes/
      Playable.unity
    Scripts/
      Core/
      Gameplay/
      UI/
      Platform/
```

Chỉ cần một scene:

1. Bootstrap các manager lean.
2. Load trực tiếp level PLA.
3. Hiện hand tutorial cho click đầu tiên.
4. Sau win: freeze/celebration ngắn rồi hiện CTA.
5. Sau lose: retry nhanh hoặc CTA, không mở shop/booster.

## Lưu ý về bản copy

- Các file được copy nguyên trạng tại thời điểm tạo folder này.
- `Assets/_Game/Script/Manager/LevelManager.cs` đang có thay đổi chưa commit trong working tree; bản ở đây phản ánh đúng phiên bản hiện tại.
- Không sửa script trong `PlayableAds_Reference` rồi kỳ vọng game gốc thay đổi; đây là snapshot độc lập.
