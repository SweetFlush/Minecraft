using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace VoxelPlay {

    public delegate void LoadGameEvent (string tag, byte[] contents);
    public delegate void SaveGameEvent (SaveGameCustomDataWriter writer);

    public partial class VoxelPlayEnvironment : MonoBehaviour {

        public event LoadGameEvent OnLoadCustomGameData;
        public event SaveGameEvent OnSaveCustomGameData;

        const string SAVEGAMEDATA_EXTENSION = ".bytes";

        /// <summary>
        /// 현재 게임이 저장 파일에서 로드된 경우 참입니다.
        /// </summary>
        [NonSerialized]
        public bool saveFileIsLoaded;


        const byte SAVE_FILE_CURRENT_FORMAT = 16;
        bool isLoadingGame;

        /// <summary>
        /// Voxel Play Environment의 "saveFilename" 속성에 지정된 저장 게임 파일을 로드합니다.
        /// </summary>
        /// <param name="preservePlayerPosition">If set to <c>진실</c> preserve player position.</param>
        /// <param name="fallbackVoxelDefinition">저장 게임 파일의 복셀 정의가 더 이상 존재하지 않는 경우 이 대체 복셀 정의로 대체됩니다.</param>
        /// <returns>저장 게임이 올바르게 로드된 경우 true</returns>
        public bool LoadGameBinary (bool preservePlayerPosition = false, VoxelDefinition fallbackVoxelDefinition = null) {

            saveFileIsLoaded = false;

            if (!CheckGameFilename())
                return false;

            bool captureChunkChangeEventsState = captureChunkChanges;

            bool result = true;
            try {
                byte[] saveGameData = GetSaveGameData();
                if (saveGameData == null) {
                    return false;
                }

                captureChunkChanges = false;

                DestroyAllVoxels();

                // get version
                isLoadingGame = true;
                using (BinaryReader br = new BinaryReader(new MemoryStream(saveGameData, false), Encoding.UTF8)) {
                    int version = br.ReadByte();
#pragma warning disable 0429
#pragma warning disable 0162
                    if (CHUNK_SIZE != 16 && version <= 9) {
                        throw new ApplicationException("Saved game cannot be loaded. Chunk size does not match!");
                    }
                    if (version >= 10) {
                        int chunkSize = br.ReadByte();
                        if (CHUNK_SIZE != chunkSize) {
                            throw new ApplicationException("Saved game cannot be loaded. Saved chunk size (" + chunkSize + ") does not match current scene chunk size!");
                        }
                    }
#pragma warning restore 0162
#pragma warning restore 0429
                    switch (version) {
                        case 5:
                            LoadGameBinaryFileFormat_5(br, preservePlayerPosition);
                            break;
                        case 6:
                            LoadGameBinaryFileFormat_6(br, preservePlayerPosition);
                            break;
                        case 7:
                            LoadGameBinaryFileFormat_7(br, preservePlayerPosition);
                            break;
                        case 8:
                            LoadGameBinaryFileFormat_8(br, preservePlayerPosition);
                            break;
                        case 9:
                            LoadGameBinaryFileFormat_9(br, preservePlayerPosition);
                            break;
                        case 10:
                            LoadGameBinaryFileFormat_10(br, preservePlayerPosition);
                            break;
                        case 11:
                            LoadGameBinaryFileFormat_11(br, preservePlayerPosition);
                            break;
                        case 12:
                            LoadGameBinaryFileFormat_12(br, preservePlayerPosition);
                            break;
                        case 13:
                            LoadGameBinaryFileFormat_13(br, preservePlayerPosition);
                            break;
                        case 14:
                            LoadGameBinaryFileFormat_14(br, preservePlayerPosition, fallbackVoxelDefinition);
                            break;
                        case 15:
                            LoadGameBinaryFileFormat_15(br, preservePlayerPosition, fallbackVoxelDefinition);
                            break;
                        case 16:
                            LoadGameBinaryFileFormat_16(br, singleFile: false, preservePlayerPosition, fallbackVoxelDefinition);
                            break;
                        default:
                            throw new ApplicationException("LoadGame() does not support this file format.");
                    }
                    br.Close();
                }
                isLoadingGame = false;
                saveFileIsLoaded = true;
                if (applicationIsPlaying && VoxelPlayUI.instance != null) {
                    VoxelPlayUI.instance.ToggleConsoleVisibility(false);
                    ShowMessage("<color=green>Game loaded successfully!</color>");
                }
                if (OnGameLoaded != null) {
                    OnGameLoaded();
                }
            }
            catch (Exception ex) {
                ShowError("<color=red>Load error:</color> <color=orange>" + ex.Message + "</color><color=white>" + ex.StackTrace + "</color>");
                result = false;
            }
            finally {
                captureChunkChanges = captureChunkChangeEventsState;
            }

            isLoadingGame = false;
            shouldCheckChunksInFrustum = true;
            return result;
        }

        string GetFullFilename (string suffix = null) {
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(world);
            path = Path.GetDirectoryName(path) + "/SavedGames";
#else
            string path = Application.persistentDataPath + "/VoxelPlay";
#endif
            Directory.CreateDirectory(path);
            path += "/" + saveFilename;
            if (!string.IsNullOrEmpty(suffix)) {
                path += "_" + suffix;
            }
            path += SAVEGAMEDATA_EXTENSION;
            return path;
        }


        byte[] GetSaveGameData (string suffix = null) {

#if UNITY_EDITOR
            // In Editor, always load saved game from Resources/Worlds/<name of world>/SavedGames folder
            string path = AssetDatabase.GetAssetPath(world);
            path = Path.GetDirectoryName(path) + "/SavedGames/" + saveFilename;
            if (!string.IsNullOrEmpty(suffix)) {
                path += "_" + suffix;
            }
            path += SAVEGAMEDATA_EXTENSION;
            if (File.Exists(path)) {
                return File.ReadAllBytes(path);
            }
            return null;

#else
												// In Build, try to load the saved game from application data path. If there's none, try to load a default saved game from Resources.
			string path = Application.persistentDataPath + "/VoxelPlay/" + saveFilename;
            if (!string.IsNullOrEmpty(suffix)) {
                path += "_" + suffix;
            }
            path += SAVEGAMEDATA_EXTENSION;
												if (File.Exists(path)) {
			return File.ReadAllBytes (path);

												} else {
                string resource = "Worlds/" + world.name + "/SavedGames/" + saveFilename;
                if (!string.IsNullOrEmpty(suffix)) {
                    resource += "_" + suffix;
                }
                TextAsset ta = Resources.Load<TextAsset>(resource);
                if (ta != null) {
                    return ta.bytes;
                } else {
                    return null;
                }
												}
#endif
        }


        bool CheckGameFilename () {
            if (string.IsNullOrEmpty(saveFilename)) {
                ShowMessage("<color=orange>Set a file name for the game to load/save first.</color>");
                return false;
            }
            return true;
        }

        public bool SaveGameBinary (bool incremental = false, bool makeBackup = false) {
            if (!CheckGameFilename())
                return false;

            bool success = true;
            try {
                RegionPartitioner regionPartitioner = new RegionPartitioner(GetChunks(ChunkModifiedFilter.OnlyModified));

                // save region files
                string regionsIds = "";
                foreach (var region in regionPartitioner.GetRegions()) {
                    string regionId = region.x + "_" + region.z;
                    if (!string.IsNullOrEmpty(regionsIds)) {
                        regionsIds += ",";
                    }
                    regionsIds += regionId;

                    string filename = GetFullFilename(regionId);
                    if (incremental && File.Exists(filename) && !region.IsModifiedSinceLastSave()) continue;

                    if (makeBackup) {
                        MakeBackup(filename);
                    }
                    FileStream fs = new FileStream(filename, FileMode.Create);
                    BinaryWriter bw = new BinaryWriter(fs, Encoding.UTF8);
                    SaveGameChunksBinaryFormat(bw, region.chunks);
                    bw.Close();
                    fs.Close();
                }

                {
                    // save header file
                    string filename = GetFullFilename();
                    if (makeBackup) {
                        MakeBackup(filename);
                    }
                    FileStream fs = new FileStream(filename, FileMode.Create);
                    BinaryWriter bw = new BinaryWriter(fs, Encoding.UTF8);
                    SaveGameHeaderBinaryFormat(bw, regionsIds);
                    bw.Close();
                    fs.Close();
                }

                {
                    // save extra data
                    string filename = GetFullFilename("extra");
                    if (makeBackup) {
                        MakeBackup(filename);
                    }
                    FileStream fs = new FileStream(filename, FileMode.Create);
                    BinaryWriter bw = new BinaryWriter(fs, Encoding.UTF8);
                    SaveGameExtraDataBinaryFormat(bw);
                    bw.Close();
                    fs.Close();
                }

                if (Application.isPlaying) {
                    ShowMessage("<color=green>Game saved successfully!</color>");
                }
            }
            catch (Exception ex) {
                ShowError("<color=red>Error:</color> <color=orange>" + ex.Message + "</color>");
                success = false;
            }
            return success;
        }

        void MakeBackup (string filename) {
            if (!File.Exists(filename))
                return;
            string backupFolder = Path.Combine(Path.GetDirectoryName(filename), "Backup");
            Directory.CreateDirectory(backupFolder);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            string backupFilename = Path.Combine(backupFolder, timestamp + "_" + Path.GetFileNameWithoutExtension(filename) + Path.GetExtension(filename));
            if (File.Exists(backupFilename)) {
                File.Delete(backupFilename);
            }
            File.Copy(filename, backupFilename);
        }

        /// <summary>
        /// 문자열로 인코딩된 세계를 반환합니다.
        /// </summary>
        /// <returns>문자로 보내는 게임.</returns>
        public byte[] SaveGameToByteArray () {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8);
            SaveGameBinaryFormat(bw);
            bw.Close();
            return ms.ToArray();
        }

        /// <summary>
        /// Base 64 형식으로 인코딩된 세계를 반환합니다.
        /// </summary>
        public string SaveGameToBase64 () {
            return Convert.ToBase64String(SaveGameToByteArray());
        }

        /// <summary>
        /// 문자열에서 게임 세계를 로드합니다.
        /// </summary>
        /// <returns>saveGameData가 성공적으로 로드된 경우 참입니다.</returns>
        /// <param name="preservePlayerPosition">If set to <c>진실</c> preserve player position.</param>
        /// <param name="clearScene">If set to <c>진실</c> existing chunks will be cleared before loading the game. If set to false, only chunks from the saved game will be replaced.</param>
        /// <param name="fallbackVoxelDefinition">저장 게임 파일의 복셀 정의가 더 이상 존재하지 않는 경우 이 대체 복셀 정의로 대체됩니다.</param>
        public bool LoadGameFromBase64 (string saveGameDataBase64string, bool preservePlayerPosition, bool clearScene = true, VoxelDefinition fallbackVoxelDefinition = null) {
            byte[] saveGameData = System.Convert.FromBase64String(saveGameDataBase64string);
            return LoadGameFromByteArray(saveGameData, preservePlayerPosition, clearScene, fallbackVoxelDefinition);
        }

        /// <summary>
        /// 문자열에서 게임 세계를 로드합니다.
        /// </summary>
        /// <returns>saveGameData가 성공적으로 로드된 경우 참입니다.</returns>
        /// <param name="preservePlayerPosition">If set to <c>진실</c> preserve player position.</param>
        /// <param name="clearScene">If set to <c>진실</c> existing chunks will be cleared before loading the game. If set to false, only chunks from the saved game will be replaced.</param>
        /// <param name="fallbackVoxelDefinition">저장 게임 파일의 복셀 정의가 더 이상 존재하지 않는 경우 이 대체 복셀 정의로 대체됩니다.</param>
        public bool LoadGameFromByteArray (byte[] saveGameData, bool preservePlayerPosition, bool clearScene = true, VoxelDefinition fallbackVoxelDefinition = null) {
            bool captureChunkChangeEventsState = captureChunkChanges;
            captureChunkChanges = false;

            if (clearScene) {
                DestroyAllVoxels();
            } else {
                // Remove all modified chunks to ensure only loaded chunks are the modified ones
                List<VoxelChunk> tempChunks = BufferPool<VoxelChunk>.Get();
                GetChunks(tempChunks, ChunkModifiedFilter.OnlyModified);
                int count = tempChunks.Count;
                for (int k = 0; k < count; k++) {
                    VoxelChunk chunk = tempChunks[k];
                    if (chunk != null && chunk.modified) {
                        // Restore original contents
                        world.terrainGenerator.PaintChunk(chunk);
                        ChunkRequestRefresh(chunk, true, true);
                        chunk.modified = false;
                        chunk.modifiedTimestamp = 0;
                    }
                }
                BufferPool<VoxelChunk>.Release(tempChunks);
            }


            bool result;
            try {
                if (saveGameData == null) {
                    return false;
                }

                // get version
                isLoadingGame = true;
                using (BinaryReader br = new BinaryReader(new MemoryStream(saveGameData), Encoding.UTF8)) {
                    byte version = br.ReadByte();
#pragma warning disable 0429
#pragma warning disable 0162
                    if (CHUNK_SIZE != 16 && version <= 9) {
                        throw new ApplicationException("Saved game cannot be loaded. Chunk size does not match!");
                    }
                    if (version >= 10) {
                        int chunkSize = br.ReadByte();
                        if (CHUNK_SIZE != chunkSize) {
                            throw new ApplicationException("Saved game cannot be loaded. Saved chunk size (" + chunkSize + ") does not match current scene chunk size!");
                        }
                    }
#pragma warning restore 0162
#pragma warning restore 0429
                    switch (version) {
                        case 5:
                            LoadGameBinaryFileFormat_5(br, preservePlayerPosition);
                            break;
                        case 6:
                            LoadGameBinaryFileFormat_6(br, preservePlayerPosition);
                            break;
                        case 7:
                            LoadGameBinaryFileFormat_7(br, preservePlayerPosition);
                            break;
                        case 8:
                            LoadGameBinaryFileFormat_8(br, preservePlayerPosition);
                            break;
                        case 9:
                            LoadGameBinaryFileFormat_9(br, preservePlayerPosition);
                            break;
                        case 10:
                            LoadGameBinaryFileFormat_10(br, preservePlayerPosition);
                            break;
                        case 11:
                            LoadGameBinaryFileFormat_11(br, preservePlayerPosition);
                            break;
                        case 12:
                            LoadGameBinaryFileFormat_12(br, preservePlayerPosition);
                            break;
                        case 13:
                            LoadGameBinaryFileFormat_13(br, preservePlayerPosition);
                            break;
                        case 14:
                            LoadGameBinaryFileFormat_14(br, preservePlayerPosition, fallbackVoxelDefinition);
                            break;
                        case 15:
                            LoadGameBinaryFileFormat_15(br, preservePlayerPosition, fallbackVoxelDefinition);
                            break;
                        case 16:
                            LoadGameBinaryFileFormat_16(br, singleFile: true, preservePlayerPosition, fallbackVoxelDefinition);
                            break;
                        default:
                            throw new ApplicationException("LoadGameFromArray() does not support this file format.");
                    }
                    br.Close();
                }
                isLoadingGame = false;
                saveFileIsLoaded = true;
                if (OnGameLoaded != null) {
                    OnGameLoaded();
                }
                result = true;
            }
            catch (Exception ex) {
                Debug.LogError("Voxel Play: " + ex.Message);
                result = false;
            }
            finally {
                captureChunkChanges = captureChunkChangeEventsState;
            }

            isLoadingGame = false;
            shouldCheckChunksInFrustum = true;
            return result;

        }


    }



}
