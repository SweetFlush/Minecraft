using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace VoxelPlay {
    public delegate void OnPlayerInventoryItemQuantityChange(ItemDefinition item, float quantity);
    public delegate void OnPlayerInventoryEvent(int selectedItemIndex, int prevSelectedItemIndex);
    public delegate void OnPlayerGetDamageEvent(ref int damage, int remainingLifePoints);
    public delegate void OnPlayerIsKilledEvent();
    public delegate void OnPlayerInventoryClear();

    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001860-voxel-play-player")]
    public partial class VoxelPlayPlayer : MonoBehaviour, IVoxelPlayPlayer {

        public event OnPlayerInventoryEvent OnItemSelectedChanged;
        public event OnPlayerGetDamageEvent OnPlayerGetDamage;
        public event OnPlayerIsKilledEvent OnPlayerIsKilled;
        public event OnPlayerInventoryItemQuantityChange OnItemAdded;
        public event OnPlayerInventoryItemQuantityChange OnItemConsumed;
        public event OnPlayerInventoryClear OnItemsClear;

        [Header("Player Info")]
        [SerializeField] protected string _playerName = "Player";
        [SerializeField] protected int _totalLife = 20;
        [SerializeField] protected bool _infiniteInventory;

        [Header("Attack")]
        [SerializeField] protected float _hitDelay = 0.2f;
        [SerializeField] protected int _hitDamage = 3;
        [SerializeField] protected float _hitRange = 5f;
        [SerializeField] protected int _hitDamageRadius = 1;

        [Header("Bare Hands")]
        public float bareHandsHitDelay = 0.2f;
        public int bareHandsHitDamage = 3;
        public float bareHandsHitRange = 5f;
        public int bareHandsHitDamageRadius = 1;

        protected int _selectedItemIndex;
        protected int _life;
        protected Color _selectedItemTintColor = Color.white;

        public virtual int life {
            get { return _life; }
            set { _life = value; }
        }

        public virtual string playerName {
            get { return _playerName; }
            set { _playerName = value; }
        }

        public virtual int totalLife {
            get { return _totalLife; }
            set { _totalLife = value; }
        }

        public virtual float hitDelay {
            get { return _hitDelay; }
            set { _hitDelay = value; }
        }

        public virtual float hitRange {
            get { return _hitRange; }
            set { _hitRange = value; }
        }

        public virtual int hitDamage {
            get { return _hitDamage; }
            set { _hitDamage = value; }
        }

        public virtual int hitDamageRadius {
            get { return _hitDamageRadius; }
            set { _hitDamageRadius = value; }
        }

        public virtual Color selectedItemTintColor {
            get { return _selectedItemTintColor; }
            set { _selectedItemTintColor = value; }
        }

        /// <summary>
        /// player.items 컬렉션에서 현재 선택된 항목의 인덱스를 가져오거나 설정합니다.
        /// </summary>
        /// <value>선택한 항목의 인덱스입니다.</value>
        public virtual int selectedItemIndex {
            get { return _selectedItemIndex; }
            set {
                if (_selectedItemIndex != value) {
                    SetSelectedItem(value);
                }
            }
        }

        /// <summary>
        /// 현재 선택된 항목(구조체임에 유의)의 복사본을 반환하거나 아무것도 선택하지 않은 경우 InventoryItem.Null을 반환합니다.
        /// </summary>
        /// <returns>선택한 항목입니다.</returns>
        public virtual InventoryItem GetSelectedItem() {
            if (this.items == null) {
                return InventoryItem.Null;
            }

            List<InventoryItem> items = this.items;
            if (_selectedItemIndex >= 0 && _selectedItemIndex < items.Count) {
                return items[_selectedItemIndex];
            }
            return InventoryItem.Null;
        }


        /// <summary>
        /// 선택한 항목을 선택 취소합니다.
        /// </summary>
        public virtual void UnSelectItem() {
            _selectedItemIndex = -1;
            ShowSelectedItem();
        }


        /// <summary>
        /// 항목 인덱스별로 선택된 항목
        /// </summary>
        public virtual bool SetSelectedItem(int itemIndex) {
            if (this.items == null) {
                return false;
            }

            if (itemIndex >= 0 && itemIndex < items.Count) {
                int prevItemIndex = _selectedItemIndex;
                _selectedItemIndex = itemIndex;
                ItemDefinition item = items[_selectedItemIndex].item;
                if (item == null) {
                    _selectedItemIndex = -1;
                    return false;
                }
                if (item.voxelType != null) {
                    selectedItemTintColor = item.voxelType.tintColor;
                } else {
                    selectedItemTintColor = Color.white;
                }
                item.GetPropertyValue<int>("hitDamage", ref _hitDamage, bareHandsHitDamage);
                item.GetPropertyValue<float>("hitDelay", ref _hitDelay, bareHandsHitDelay);
                item.GetPropertyValue<float>("hitRange", ref _hitRange, bareHandsHitRange);
                item.GetPropertyValue<int>("hitDamageRadius", ref _hitDamageRadius, bareHandsHitDamageRadius);

                ShowSelectedItem();
                if (OnItemSelectedChanged != null) {
                    OnItemSelectedChanged(_selectedItemIndex, prevItemIndex);
                }
            }
            return true;
        }

        public virtual void DamageToPlayer(int damagePoints) {
            if (damagePoints > 0 && OnPlayerGetDamage != null) {
                OnPlayerGetDamage(ref damagePoints, _life - damagePoints);
            }
            _life -= damagePoints;
            if (_life <= 0) {
                if (OnPlayerIsKilled != null)
                    OnPlayerIsKilled();
            }
        }



        /// <summary>
        /// 항목 개체별로 항목을 선택합니다.
        /// </summary>
        /// <param name="item">목.</param>
        public virtual bool SetSelectedItem(InventoryItem item) {
            if (this.items == null) {
                return false;
            }

            List<InventoryItem> items = this.items;

            int count = items.Count;
            for (int k = 0; k < count; k++) {
                if (items[k] == item) {
                    selectedItemIndex = k;
                    if (item.item.voxelType != null) {
                        selectedItemTintColor = item.item.voxelType.tintColor;
                    } else {
                        selectedItemTintColor = Color.white;
                    }
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// 복셀 정의 유형에 따라 인벤토리에서 항목을 선택합니다.
        /// </summary>
        public virtual bool SetSelectedItem(VoxelDefinition vd) {
            if (this.items == null) {
                return false;
            }

            List<InventoryItem> items = this.items;
            int count = items.Count;
            for (int k = 0; k < count; k++) {
                InventoryItem item = items[k];
                if (item.item.category == ItemCategory.Voxel && item.item.voxelType == vd) {
                    selectedItemIndex = k;
                    selectedItemTintColor = vd.tintColor;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 현재 사용 가능한 항목 목록을 반환합니다.빌드 모드가 ON이면 모든 월드 아이템을 반환합니다.빌드 모드가 OFF이면 playerItems를 반환합니다.
        /// </summary>
        /// <value>현재 항목입니다.</value>
        public virtual List<InventoryItem> items {
            get {
                VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
                if (env != null && env.buildMode) {
                    return env.allItems;
                }
                return playerItems;
            }
        }


        /// <summary>
        /// 플레이어 인벤토리의 항목 목록(Inspector에 정의됨)
        /// </summary>
        [Header("Items")]
        public List<InventoryItem> playerItems;

        /// <summary>
        /// playerItems의 동의어
        /// </summary>
        public virtual List<InventoryItem> GetPlayerItems() {
            return playerItems;
        }

        AudioSource _audioSource;

        /// <summary>
        /// 플레이어 게임 객체에 연결된 AudioSource 구성 요소를 반환합니다.
        /// </summary>
        /// <value>오디오 소스입니다.</value>
        public virtual AudioSource audioSource {
            get {
                if (_audioSource == null) {
                    _audioSource = transform.root.GetComponentInChildren<AudioSource>(true);
                }
                return _audioSource;
            }
        }

        static IVoxelPlayPlayer _player;

        /// <summary>
        /// 플레이어 구성 요소에 대한 참조를 가져옵니다.플레이어 구성 요소에는 이름, 수명, 인벤토리와 같은 정보가 포함됩니다.
        /// </summary>
        /// <value>인스턴스.</value>
        public static IVoxelPlayPlayer instance {
            get {
                if (_player == null || _player.Equals(null)) {
                    VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
                    if (env != null && env.playerGameObject != null) {
                        _player = env.playerGameObject.GetComponentInChildren<IVoxelPlayPlayer>();
                        if (_player == null) {
                            _player = env.playerGameObject.AddComponent<VoxelPlayPlayer>();
                        }
                    }
                }
                return _player;
            }
            set {
                _player = value;
            }
        }


        void OnEnable() {
            _life = _totalLife;
            InitPlayerInventory();
        }


        protected virtual void InitPlayerInventory() {
            if (items == null) {
                playerItems = new List<InventoryItem>(250);
            }

            _selectedItemIndex = -1;
            ShowSelectedItem();
        }


        protected virtual void ShowSelectedItem() {
            if (items == null) {
                return;
            }
            VoxelPlayUI ui = VoxelPlayUI.instance;
            if (ui != null) {
                if (_selectedItemIndex >= 0 && _selectedItemIndex < items.Count) {
                    ui.ShowSelectedItem(items[_selectedItemIndex]);
                } else {
                    ui.HideSelectedItem();
                }
            }
        }

        /// <summary>
        /// 인벤토리에 다양한 아이템을 추가합니다.
        /// </summary>
        /// <param name="newItems">품목.</param>
        public virtual void AddInventoryItem(ItemDefinition[] newItems) {
            if (newItems == null) {
                return;
            }

            for (int k = 0; k < newItems.Length; k++) {
                AddInventoryItem(newItems[k]);
            }
        }


        /// <summary>
        /// 캐릭터가 장면에서 객체를 선택할 때 호출됩니다.
        /// </summary>
        /// <param name="newItem"></param>
        /// <param name="quantity"></param>
        public virtual void PickUpItem(ItemDefinition newItem, float quantity = 1) {
            if (newItem == null || quantity <= 0) return;

            if (audioSource != null) {
                if (newItem.pickupSound != null) {
                    audioSource.PlayOneShot(newItem.pickupSound);
                } else if (VoxelPlayEnvironment.instance.defaultPickupSound != null) {
                    audioSource.PlayOneShot(VoxelPlayEnvironment.instance.defaultPickupSound);
                }
            }
            AddInventoryItem(newItem, quantity);
        }


        /// <summary>
        /// 인벤토리에 새 항목을 추가합니다.
        /// </summary>
        public virtual bool AddInventoryItem(ItemDefinition newItem, float quantity = 1) {
            if (newItem == null || items == null) {
                return false;
            }

            // Check if item is already in inventory
            int itemsCount = items.Count;
            InventoryItem i;
            for (int k = 0; k < itemsCount; k++) {
                if (items[k].item == newItem) {
                    i = items[k];
                    i.quantity += quantity;
                    items[k] = i;
                    if (OnItemAdded != null) OnItemAdded(newItem, quantity);
                    ShowSelectedItem();
                    return false;
                }
            }
            i = new InventoryItem();
            i.item = newItem;
            i.quantity = quantity;
            items.Add(i);
            if (OnItemAdded != null) OnItemAdded(newItem, quantity);

            if (_selectedItemIndex < 0) {
                selectedItemIndex = items.Count - 1;
                ShowSelectedItem();
            }

            return true;
        }

        /// <summary>
        /// 무기 명중 사이의 지연
        /// </summary>
        public virtual float GetHitDelay() {
            return hitDelay;
        }

        /// <summary>
        /// 무기 명중 사이의 지연
        /// </summary>
        public virtual float GetHitRange() {
            return hitRange;
        }


        /// <summary>
        /// 현재 적중 피해를 받으세요
        /// </summary>
        public virtual int GetHitDamage() {
            return hitDamage;
        }



        /// <summary>
        /// 현재 적중 피해 반경을 가져옵니다.
        /// </summary>
        public virtual int GetHitDamageRadius() {
            return hitDamageRadius;
        }


        /// <summary>
        /// 현재 선택한 항목에서 한 단위를 줄이고 InventoryItem 또는 InventoryItem.Null의 복사본을 반환합니다(아무 것도 선택하지 않은 경우).
        /// </summary>
        public virtual InventoryItem ConsumeItem() {

            if (this.items == null) {
                return InventoryItem.Null;
            }

            List<InventoryItem> items = this.items;

            if (_selectedItemIndex >= 0 && _selectedItemIndex < items.Count) {
                InventoryItem i = items[_selectedItemIndex];
                if (!_infiniteInventory) {
                    if (i.quantity <= 1) {
                        items.RemoveAt(_selectedItemIndex);
                        selectedItemIndex = 0;
                        i.quantity = 0;
                    } else {
                        i.quantity--;
                        items[_selectedItemIndex] = i; // update back because it's a struct
                    }
                }
                if (OnItemConsumed != null) OnItemConsumed(i.item, i.quantity);
                ShowSelectedItem();
                return i;
            }
            return InventoryItem.Null;
        }

        public virtual void ConsumeAllItems() {
            if (playerItems == null) return;
            if (OnItemsClear != null) OnItemsClear();
            playerItems.Clear();
            UnSelectItem();
        }

        /// <summary>
        /// Reduces custom amount from player inventory. 
        /// </summary>
        /// <param name="item">목.</param>
        public virtual void ConsumeItem(ItemDefinition item) {
            ConsumeItem(item, 1);
        }

        /// <summary>
        /// Reduces one unit from player inventory. 
        /// </summary>
        /// <param name="item">목.</param>
        public virtual void ConsumeItem(ItemDefinition item, float amount) {
            if (items == null || item == null) {
                return;
            }

            int itemCount = items.Count;
            for (int k = 0; k < itemCount; k++) {
                if (items[k].item == item) {
                    InventoryItem i = items[_selectedItemIndex];
                    i.quantity -= amount;
                    if (i.quantity <= 0) {
                        items.RemoveAt(k);
                        selectedItemIndex = 0;
                    } else {
                        items[_selectedItemIndex] = i; // update back because it's a struct
                    }
                    if (OnItemConsumed != null) OnItemConsumed(i.item, i.quantity);
                    break;
                }
            }
            ShowSelectedItem();
        }


        /// <summary>
        /// 플레이어가 인벤토리에 이 항목을 가지고 있으면 true를 반환합니다.
        /// </summary>
        public virtual bool HasItem(ItemDefinition item) {
            return GetItemQuantity(item) > 0;
        }



        /// <summary>
        /// 플레이어가 가지고 있는 ItemDefinition의 단위 수를 반환합니다(있는 경우).
        /// </summary>
        public virtual float GetItemQuantity(ItemDefinition item) {
            if (items == null || item == null) {
                return 0;
            }

            int itemCount = items.Count;
            for (int k = 0; k < itemCount; k++) {
                if (items[k].item == item) {
                    InventoryItem i = items[_selectedItemIndex];
                    return i.quantity;
                }
            }
            return 0;
        }



        /// <summary>
        /// 주어진 복셀 정의에 따라 인벤토리 항목을 반환합니다.
        /// </summary>
        public virtual InventoryItem GetInventoryItem(VoxelDefinition voxelDefinition) {
            if (items == null || voxelDefinition == null) {
                return InventoryItem.Null;
            }

            int itemCount = items.Count;
            for (int k = 0; k < itemCount; k++) {
                if (items[k].item.voxelType == voxelDefinition) {
                    InventoryItem i = items[k];
                    return i;
                }
            }
            return InventoryItem.Null;
        }

        public virtual Transform GetTransform() {
            return transform;
        }


    }
}
