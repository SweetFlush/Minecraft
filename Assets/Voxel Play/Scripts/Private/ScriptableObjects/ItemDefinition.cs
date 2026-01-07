using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    public enum ItemCategory {
        Voxel = 0,
        Torch = 1,
        Model = 2,
        General = 10
    }

    public enum PickingMode {
        PickOnApproach = 0,
        PickOnClick = 1
    }

    [Serializable]
    public struct ItemProperty {
        public string name;
        public string value;
    }

    [CreateAssetMenu(menuName = "Voxel Play/Item Definition", fileName = "ItemDefinition", order = 104)]
    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000051366-item-definitions")]
    public partial class ItemDefinition : ScriptableObject {
        public string title;
        public ItemCategory category;

        [Tooltip("이 항목과 관련된 복셀 정의입니다.")]
        public VoxelDefinition voxelType;

        [Tooltip("이 항목과 관련된 모델 정의입니다.")]
        public ModelDefinition model;

        [Tooltip("인벤토리 패널에 사용되는 아이콘입니다.")]
        public Texture2D icon;

        [Tooltip("이 아이템을 휘두르는 동안 인벤토리 패널이나 캐릭터 손에 사용할 수 있는 구조물입니다.")]
        public GameObject iconPrefab;

        [Tooltip("아이콘의 선택적 색조 색상입니다.")]
        public Color32 color = Misc.color32White;

        [Tooltip("이 아이템을 던지거나 떨어뜨리거나 장면에 일반 게임 개체(예: 횃불)로 배치할 때 사용되는 프리팹")]
        public GameObject prefab;

        [Tooltip("추가/선택적 프리팹")]
        public GameObject prefab2;

        [Tooltip("추가/선택적 프리팹")]
        public GameObject prefab3;

        [Tooltip("플레이어가 이 아이템을 사용하여 공격할 때 재생되는 소리")]
        public AudioClip useSound;

        [Tooltip("이 항목을 현장에서 선택할 수 있는 경우")]
        public bool canBePicked = true;

        [Tooltip("이 항목을 선택하는 방법")]
        public PickingMode pickMode;

        [Tooltip("장면에서 항목을 선택할 때 재생되는 소리")]
        public AudioClip pickupSound;

        [Tooltip("사용자 정의 항목 속성.")]
        public ItemProperty[] properties;

        [Range(0, 15), Tooltip("Intensity of emitted light")]
        public byte lightIntensity = 15;

        public static readonly string[] commonProperties = {
            "hitDamage",
            "hitDelay",
            "hitRange",
            "hitDamageRadius",
            "weaponType",
            "value",
            "weight",
            "rarity",
            "healthPoints",
            "microVoxels",
            "microVoxelsProb",
            "(user defined)"
        };

        public static string hitDamage = commonProperties[0];
        public static string hitDelay = commonProperties[1];
        public static string hitRange = commonProperties[2];
        public static string hitDamageRadius = commonProperties[3];
        public static string weaponType = commonProperties[4];
        public static string value = commonProperties[5];
        public static string weight = commonProperties[6];
        public static string rarity = commonProperties[7];
        public static string healthPoints = commonProperties[8];


        public string GetTitleOrName() {
            if (string.IsNullOrEmpty(title)) return name;
            return title;
        }

        public T GetPropertyValue<T>(string name, T defaultValue = default) {
            if (properties == null)
                return defaultValue;
            name = name.ToUpper();
            for (int k = 0; k < properties.Length; k++) {
                if (properties[k].name.ToUpper().Equals(name)) {
                    switch (Type.GetTypeCode(typeof(T))) {
                        case TypeCode.Int32:
                            return (T)(object)Convert.ToInt32(properties[k].value, System.Globalization.CultureInfo.InvariantCulture);
                        case TypeCode.String:
                            return (T)(object)properties[k].value;
                        case TypeCode.Single:
                            return (T)(object)Convert.ToSingle(properties[k].value, System.Globalization.CultureInfo.InvariantCulture);
                        default:
                            Debug.LogError("Only int, float or string types are supported.");
                            break;
                    }
                    break;
                }
            }
            return defaultValue;
        }


        public bool GetPropertyValue<T>(string name, ref T value, T defaultValue) {
            if (properties == null)
                return false;
            name = name.ToUpper();
            for (int k = 0; k < properties.Length; k++) {
                if (properties[k].name.ToUpper().Equals(name)) {
                    switch (Type.GetTypeCode(typeof(T))) {
                        case TypeCode.Int32:
                            value = (T)(object)Convert.ToInt32(properties[k].value, System.Globalization.CultureInfo.InvariantCulture);
                            return true;
                        case TypeCode.String:
                            value = (T)(object)properties[k].value;
                            return true;
                        case TypeCode.Single:
                            value = (T)(object)Convert.ToSingle(properties[k].value, System.Globalization.CultureInfo.InvariantCulture);
                            return true;
                        default:
                            Debug.LogError("Only int, float or string types are supported.");
                            value = defaultValue;
                            return false;
                    }
                }
            }
            value = defaultValue;
            return false;
        }


        public void SetPropertyValue(string name, string value) {
            if (properties == null) {
                properties = new ItemProperty[0];
            }
            string nameCheck = name.ToUpper();
            for (int k = 0; k < properties.Length; k++) {
                if (properties[k].name.ToUpper().Equals(nameCheck)) {
                    properties[k].value = value;
                    return;
                }
            }
            int length = properties.Length;
            Array.Resize(ref properties, length + 1);
            properties[length].name = name;
            properties[length].value = value;

        }
    }
}