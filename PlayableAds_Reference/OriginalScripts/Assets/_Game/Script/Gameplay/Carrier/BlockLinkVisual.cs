using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class BlockLinkVisual : MonoBehaviour
{
    [SerializeField] private float linkWidth = 0.15f;
    [SerializeField] private float lengthScale = 1f;
    [SerializeField] private float lengthOffset = 0f;

    [Header("VFX Settings")]
    [SerializeField] private ParticleSystem vfxSplashDecayPrefab;
    [SerializeField] private float vfxSpacing = 1.5f;

    private CarrierBase _carrierA;
    private Block _blockA;
    private CarrierBase _carrierB;
    private Block _blockB;

    public Block BlockA => _blockA;
    public Block BlockB => _blockB;

    private int _initialCubesA;
    private int _initialCubesB;

    private static readonly int LeftColor = Shader.PropertyToID("_LeftColor");
    private static readonly int RightColor = Shader.PropertyToID("_RightColor");
    private static readonly int LeftShadowColor = Shader.PropertyToID("_LeftShadowColor");
    private static readonly int RightShadowColor = Shader.PropertyToID("_RightShadowColor");
    private static readonly int LeftOutlineColor = Shader.PropertyToID("_LeftOutlineColor");
    private static readonly int RightOutlineColor = Shader.PropertyToID("_RightOutlineColor");

    private MaterialPropertyBlock _materialBlock;

    public void Setup(CarrierBase carrierA, Block blockA, CarrierBase carrierB, Block blockB)
    {
        _carrierA = carrierA;
        _blockA = blockA;
        _carrierB = carrierB;
        _blockB = blockB;

        _initialCubesA = GetCurrentCubesInRun(_carrierA, _blockA);
        _initialCubesB = GetCurrentCubesInRun(_carrierB, _blockB);
        
        SetupMaterialColors();
        UpdatePositions();
    }

    private List<Block> GetUnloadingOrActiveSameColorRun(CarrierBase carrier, Block startBlock)
    {
        var run = new List<Block>();
        if (carrier == null || startBlock == null || !startBlock.HasContent) return run;

        var blockController = carrier.BlockController;
        if (blockController == null) return run;

        var layout = carrier.BlockLayout;
        if (layout == null) return run;

        var startIdx = blockController.GetBlockIndex(startBlock);
        if (startIdx < 0) return run;

        var colorType = startBlock.GetBlockColorType();
        var maxBlockCount = carrier.MaxBlockCount;

        // Quét ngược xuống đáy (chỉ số index giảm dần)
        for (int i = startIdx; i >= 0; i--)
        {
            var block = layout.GetBlockByIndex(i);
            if (block != null && block.HasContent && block.GetBlockColorType() == colorType)
                run.Add(block);
            else
                break;
        }

        // Quét xuôi lên đỉnh (chỉ số index tăng dần)
        for (int i = startIdx + 1; i < maxBlockCount; i++)
        {
            var block = layout.GetBlockByIndex(i);
            if (block != null && block.HasContent && block.GetBlockColorType() == colorType)
                run.Add(block);
            else
                break;
        }

        return run;
    }

    private int GetCurrentCubesInRun(CarrierBase carrier, Block block)
    {
        var run = GetUnloadingOrActiveSameColorRun(carrier, block);
        int total = 0;
        foreach (var b in run)
        {
            if (b != null)
            {
                total += b.GetCurrentCubes();
            }
        }
        return total;
    }

    private void SetupMaterialColors()
    {
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        var entryA = GetColorEntry(_carrierA, _blockA);
        var entryB = GetColorEntry(_carrierB, _blockB);

        _materialBlock ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(_materialBlock);

        // Set màu cho đầu bên A (sử dụng thuộc tính Right)
        if (entryA != null)
        {
            _materialBlock.SetColor(RightColor, entryA.Color);
            _materialBlock.SetColor(RightShadowColor, entryA.ShadowColor);
            _materialBlock.SetColor(RightOutlineColor, entryA.OutlineColor);
        }

        // Set màu cho đầu bên B (sử dụng thuộc tính Left)
        if (entryB != null)
        {
            _materialBlock.SetColor(LeftColor, entryB.Color);
            _materialBlock.SetColor(LeftShadowColor, entryB.ShadowColor);
            _materialBlock.SetColor(LeftOutlineColor, entryB.OutlineColor);
        }

        renderer.SetPropertyBlock(_materialBlock);
    }

    private ColorEntry GetColorEntry(CarrierBase carrier, Block block)
    {
        if (block == null) return null;
        EBlockColorType colorType = block.GetBlockColorType();
        if (colorType == EBlockColorType.None) return null;

        if (carrier != null && carrier.ColorConfig != null)
        {
            return carrier.ColorConfig.GetColorEntry(colorType);
        }

        if (ConfigManager.Instance != null)
        {
            var config = ConfigManager.Instance.GetColorConfig();
            if (config != null)
            {
                return config.GetColorEntry(colorType);
            }
        }

        return null;
    }


    [Button]
    public void UpdatePositions()
    {
        if (_carrierA == null || _blockA == null || _carrierB == null || _blockB == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // Ẩn line kết nối ngay lập tức nếu một trong các block không còn content, hoặc mất link
        if (!_blockA.HasContent || !_blockB.HasContent || 
            !_blockA.HasLinkGroupId() || !_blockB.HasLinkGroupId())
        {
            if (gameObject.activeSelf)
            {
                PlayUnloadVFX();
            }
            gameObject.SetActive(false);
            return;
        }

        bool isUnloading = _blockA.IsOpened || _blockB.IsOpened;
        if (isUnloading)
        {
            int currentCubesA = GetCurrentCubesInRun(_carrierA, _blockA);
            int currentCubesB = GetCurrentCubesInRun(_carrierB, _blockB);

            int unloadedA = Mathf.Max(0, _initialCubesA - currentCubesA);
            int unloadedB = Mathf.Max(0, _initialCubesB - currentCubesB);

            bool hideLink = false;
            if (_initialCubesA <= _initialCubesB)
            {
                // A là khối nhỏ nhất
                if (_initialCubesA <= 0 || unloadedA >= _initialCubesA / 2f)
                {
                    hideLink = true;
                }
            }
            else
            {
                // B là khối nhỏ nhất
                if (_initialCubesB <= 0 || unloadedB >= _initialCubesB / 2f)
                {
                    hideLink = true;
                }
            }

            if (hideLink)
            {
                if (gameObject.activeSelf)
                {
                    PlayUnloadVFX();
                }
                gameObject.SetActive(false);
                return;
            }
        }
        else
        {
            _initialCubesA = GetCurrentCubesInRun(_carrierA, _blockA);
            _initialCubesB = GetCurrentCubesInRun(_carrierB, _blockB);
        }

        gameObject.SetActive(true);

        // Lấy tọa độ neo phù hợp của bên A và bên B (tự động nhận biết gộp mesh2, mesh3)
        Vector3 posALeft = GetActiveAnchorPosition(_carrierA, _blockA, true);
        Vector3 posARight = GetActiveAnchorPosition(_carrierA, _blockA, false);
        Vector3 posBLeft = GetActiveAnchorPosition(_carrierB, _blockB, true);
        Vector3 posBRight = GetActiveAnchorPosition(_carrierB, _blockB, false);

        // So sánh khoảng cách 4 cặp điểm để chọn cặp gần nhau nhất
        float d_LL = Vector3.Distance(posALeft, posBLeft);
        float d_LR = Vector3.Distance(posALeft, posBRight);
        float d_RL = Vector3.Distance(posARight, posBLeft);
        float d_RR = Vector3.Distance(posARight, posBRight);

        float minDistance = d_LL;
        Vector3 finalPosA = posALeft;
        Vector3 finalPosB = posBLeft;

        if (d_LR < minDistance)
        {
            minDistance = d_LR;
            finalPosA = posALeft;
            finalPosB = posBRight;
        }
        if (d_RL < minDistance)
        {
            minDistance = d_RL;
            finalPosA = posARight;
            finalPosB = posBLeft;
        }
        if (d_RR < minDistance)
        {
            minDistance = d_RR;
            finalPosA = posARight;
            finalPosB = posBRight;
        }

        // Định vị trung điểm
        Vector3 mid = (finalPosA + finalPosB) * 0.5f;
        transform.position = mid;

        // Xoay hướng chỉ về điểm B
        Vector3 dir = (finalPosB - finalPosA).normalized;
        if (dir != Vector3.zero)
        {
            transform.right = dir;
        }

        // Tỉ lệ khoảng cách và độ dày của dây liên kết
        float length = Vector3.Distance(finalPosA, finalPosB) * lengthScale + lengthOffset;
        length = Mathf.Max(0f, length);
        Vector3 scale = transform.localScale;
        scale.x = length;
        scale.y = linkWidth;
        scale.z = linkWidth;
        transform.localScale = scale;
    }

    private Vector3 GetActiveAnchorPosition(CarrierBase carrier, Block block, bool isLeft)
    {
        if (carrier == null || block == null) return Vector3.zero;

        // Nếu block đang thuộc về một nhóm gộp visual (mesh2, mesh3, mesh4)
        if (carrier.LinkedBlockVisualController != null)
        {
            var linkedVisual = carrier.LinkedBlockVisualController.GetLinkedVisualContainingBlock(block);
            if (linkedVisual != null)
            {
                Transform linkedAnchor = isLeft ? linkedVisual.LeftLinkAnchor : linkedVisual.RightLinkAnchor;
                if (linkedAnchor != null)
                {
                    return linkedAnchor.position; // Lấy neo do kéo thả trên prefab gộp
                }
            }
        }

        // Dự phòng 1: Dùng neo của chính block đơn lẻ (1x)
        Transform blockAnchor = isLeft ? block.LeftLinkAnchor : block.RightLinkAnchor;
        if (blockAnchor != null)
        {
            return blockAnchor.position;
        }

        // Dự phòng 2: Dùng tâm của block
        return block.AnimationPivot != null ? block.AnimationPivot.position : block.transform.position;
    }

    public void PlayUnloadVFX()
    {
        if (vfxSplashDecayPrefab == null) return;

        // Lấy thông tin màu sắc để phối màu
        var entryA = GetColorEntry(_carrierA, _blockA);
        var entryB = GetColorEntry(_carrierB, _blockB);
        Color colorA = entryA != null ? entryA.Color : Color.white;
        Color colorB = entryB != null ? entryB.Color : Color.white;

        // Tính toán tọa độ neo và độ dài hiện tại
        Vector3 posALeft = GetActiveAnchorPosition(_carrierA, _blockA, true);
        Vector3 posARight = GetActiveAnchorPosition(_carrierA, _blockA, false);
        Vector3 posBLeft = GetActiveAnchorPosition(_carrierB, _blockB, true);
        Vector3 posBRight = GetActiveAnchorPosition(_carrierB, _blockB, false);

        float d_LL = Vector3.Distance(posALeft, posBLeft);
        float d_LR = Vector3.Distance(posALeft, posBRight);
        float d_RL = Vector3.Distance(posARight, posBLeft);
        float d_RR = Vector3.Distance(posARight, posBRight);

        float minDistance = d_LL;
        Vector3 finalPosA = posALeft;
        Vector3 finalPosB = posBLeft;

        if (d_LR < minDistance)
        {
            minDistance = d_LR;
            finalPosA = posALeft;
            finalPosB = posBRight;
        }
        if (d_RL < minDistance)
        {
            minDistance = d_RL;
            finalPosA = posARight;
            finalPosB = posBLeft;
        }
        if (d_RR < minDistance)
        {
            minDistance = d_RR;
            finalPosA = posARight;
            finalPosB = posBRight;
        }

        float length = Vector3.Distance(finalPosA, finalPosB) * lengthScale + lengthOffset;
        length = Mathf.Max(0f, length);

        int count = vfxSpacing > 0 ? Mathf.Max(2, Mathf.RoundToInt(length / vfxSpacing)) : 2;

        float startLocalX = -length / 2.0f;
        float deltaX = count > 1 ? length / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float localX = count > 1 ? startLocalX + i * deltaX : 0f;
            Vector3 worldPos = transform.TransformPoint(new Vector3(localX, 0f, 0f));

            ParticleSystem vfxInstance = null;
            if (Application.isPlaying && PoolManagerNew.Instance != null)
            {
                vfxInstance = PoolManagerNew.Instance.PopFromPool(vfxSplashDecayPrefab);
                if (vfxInstance != null)
                {
                    vfxInstance.transform.position = worldPos;
                    vfxInstance.transform.rotation = transform.rotation;
                }
            }
            else
            {
                var go = Instantiate(vfxSplashDecayPrefab.gameObject, worldPos, transform.rotation);
                vfxInstance = go.GetComponent<ParticleSystem>();
            }

            if (vfxInstance != null)
            {
                // Set thẳng màu theo bên trái (colorB) hoặc bên phải (colorA)
                // Lưu ý: trục local X dương hướng từ A sang B nên localX >= 0 tương ứng với bên trái (colorB)
                Color vfxColor = localX >= 0f ? colorB : colorA;

                // Cấu hình màu cho Particle System chính và các con
                var particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in particleSystems)
                {
                    var main = ps.main;
                    main.startColor = vfxColor;

                    var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (psRenderer != null)
                    {
                        var mpb = new MaterialPropertyBlock();
                        psRenderer.GetPropertyBlock(mpb);
                        mpb.SetColor("_Color", vfxColor);
                        psRenderer.SetPropertyBlock(mpb);
                    }
                }

                vfxInstance.Play();

                if (Application.isPlaying)
                {
                    AutoReturnVFX(vfxInstance).Forget();
                }
            }
        }
    }

    private async UniTaskVoid AutoReturnVFX(ParticleSystem psInstance)
    {
        if (psInstance == null) return;
        var main = psInstance.main;
        float duration = main.duration + main.startLifetime.constantMax;

        await UniTask.Delay(System.TimeSpan.FromSeconds(duration));

        if (psInstance != null && PoolManagerNew.Instance != null)
        {
            PoolManagerNew.Instance.PushToPool(psInstance);
        }
    }
}
