using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class BubbleLevelEditorWindow : EditorWindow
{
    private const int MinimumShotCount = 1;
    private const int MaximumShotCount = 100;
    private const float CellSize = 30f;
    private const float CellGap = 3f;
    private const string PreviewName = "Level Preview";

    private readonly BubbleColor[] palette =
    {
        BubbleColor.Empty,
        BubbleColor.Red,
        BubbleColor.Blue,
        BubbleColor.Green,
        BubbleColor.Yellow
    };

    private LevelData level;
    private SerializedObject levelObject;
    private Vector2 scrollPosition;
    private BubbleColor paintColor = BubbleColor.Red;
    private bool livePreview = true;
    private Color redDisplayColor = Color.red;
    private Color blueDisplayColor = Color.blue;
    private Color greenDisplayColor = Color.green;
    private Color yellowDisplayColor = Color.yellow;
    private GameObject previewRoot;
    private int lastPaintedItem = -1;

    [MenuItem("Tools/Bubble Shooter/Level Editor")]
    public static void Open()
    {
        GetWindow<BubbleLevelEditorWindow>("Bubble Level Editor");
    }

    private void OnEnable()
    {
        minSize = new Vector2(470f, 620f);
        Undo.undoRedoPerformed += HandleUndoRedo;
        AssemblyReloadEvents.beforeAssemblyReload += RemovePreview;
        EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        TryUseSceneLevel();
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= HandleUndoRedo;
        AssemblyReloadEvents.beforeAssemblyReload -= RemovePreview;
        EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
        RemovePreview();
    }

    private void OnGUI()
    {
        HandleMouseRelease();
        DrawHeader();

        if (level == null)
        {
            EditorGUILayout.HelpBox("Choose a level asset or use the level assigned to MapLoader.", MessageType.Info);
            return;
        }

        EnsureSerializedObject();
        levelObject.Update();
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawBubbleColors();
        EditorGUILayout.Space(12f);
        DrawPalette();
        DrawMap();
        DrawRowControls();
        EditorGUILayout.Space(12f);
        DrawShotSequence();
        EditorGUILayout.Space(12f);
        DrawValidation();
        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Bubble Level", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        LevelData chosenLevel = (LevelData)EditorGUILayout.ObjectField("Level Asset", level, typeof(LevelData), false);

        if (EditorGUI.EndChangeCheck())
        {
            SetLevel(chosenLevel);
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Use Scene Level"))
        {
            TryUseSceneLevel();
        }

        using (new EditorGUI.DisabledScope(level == null))
        {
            if (GUILayout.Button("Assign To MapLoader"))
            {
                AssignToMapLoader();
            }

            if (GUILayout.Button("Duplicate Level"))
            {
                DuplicateLevel();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        livePreview = EditorGUILayout.ToggleLeft("Live Scene Preview", livePreview);

        if (EditorGUI.EndChangeCheck())
        {
            RefreshPreview();
        }

        EditorGUILayout.Space(8f);
    }

    private void DrawBubbleColors()
    {
        EditorGUILayout.LabelField("Bubble Colors", EditorStyles.boldLabel);

        BubbleView prefab = FindBubblePrefab();

        if (prefab == null)
        {
            EditorGUILayout.HelpBox("Assign a Bubble prefab to MapLoader to edit its colors.", MessageType.Info);
            return;
        }

        SerializedObject bubbleObject = new SerializedObject(prefab);
        bubbleObject.Update();
        SerializedProperty redColor = bubbleObject.FindProperty("redColor");
        SerializedProperty blueColor = bubbleObject.FindProperty("blueColor");
        SerializedProperty greenColor = bubbleObject.FindProperty("greenColor");
        SerializedProperty yellowColor = bubbleObject.FindProperty("yellowColor");

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(redColor);
        EditorGUILayout.PropertyField(blueColor);
        EditorGUILayout.PropertyField(greenColor);
        EditorGUILayout.PropertyField(yellowColor);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(prefab, "Change Bubble Colors");
            bubbleObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(prefab);
            PrefabUtility.SavePrefabAsset(prefab.gameObject);
            RefreshPreview();
        }

        redDisplayColor = redColor.colorValue;
        blueDisplayColor = blueColor.colorValue;
        greenDisplayColor = greenColor.colorValue;
        yellowDisplayColor = yellowColor.colorValue;
    }
    private void DrawPalette()

    {
        EditorGUILayout.LabelField("Paint Color", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        foreach (BubbleColor color in palette)
        {
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = GetDisplayColor(color);
            string label = paintColor == color ? $"[{GetLetter(color)}]" : GetLetter(color);

            if (GUILayout.Button(label, GUILayout.Width(54f), GUILayout.Height(28f)))
            {
                paintColor = color;
                Repaint();
            }

            GUI.backgroundColor = oldColor;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Left click or drag paints. Right click or drag erases.", EditorStyles.miniLabel);
        EditorGUILayout.Space(8f);
    }

    private void DrawMap()
    {
        SerializedProperty columns = levelObject.FindProperty("columns");
        SerializedProperty rows = levelObject.FindProperty("rows");
        EditorGUILayout.LabelField($"Map   {columns.intValue} columns × {rows.arraySize} rows", EditorStyles.boldLabel);

        for (int row = 0; row < rows.arraySize; row++)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, CellSize);
            float rowOffset = row % 2 == 0 ? 0f : (CellSize + CellGap) * 0.5f;
            float startX = rowRect.x + 28f + rowOffset;
            GUI.Label(new Rect(rowRect.x, rowRect.y + 6f, 24f, 20f), (row + 1).ToString(), EditorStyles.miniLabel);
            SerializedProperty cells = rows.GetArrayElementAtIndex(row).FindPropertyRelative("cells");

            for (int column = 0; column < columns.intValue; column++)
            {
                bool valid = row % 2 == 0 || column < columns.intValue - 1;
                Rect cellRect = new Rect(startX + column * (CellSize + CellGap), rowRect.y, CellSize, CellSize);
                DrawCell(cellRect, cells.GetArrayElementAtIndex(column), row, column, valid);
            }
        }
    }

    private void DrawCell(Rect rect, SerializedProperty cell, int row, int column, bool valid)
    {
        BubbleColor color = valid ? (BubbleColor)cell.enumValueIndex : BubbleColor.Empty;
        Color oldColor = GUI.backgroundColor;
        GUI.backgroundColor = valid ? GetDisplayColor(color) : new Color(0.2f, 0.2f, 0.2f);
        GUI.Box(rect, valid ? GetLetter(color) : "×", EditorStyles.miniButton);
        GUI.backgroundColor = oldColor;

        if (!valid)
        {
            return;
        }

        Event currentEvent = Event.current;

        if (!rect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        if (currentEvent.type != EventType.MouseDown && currentEvent.type != EventType.MouseDrag)
        {
            return;
        }

        int item = row * 1000 + column;

        if (item == lastPaintedItem)
        {
            return;
        }

        BubbleColor newColor = currentEvent.button == 1 ? BubbleColor.Empty : paintColor;
        SetEnumValue(cell, newColor, $"Paint Bubble {row + 1}-{column + 1}");
        lastPaintedItem = item;
        currentEvent.Use();
    }

    private void DrawRowControls()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Row"))
        {
            AddRow();
        }

        SerializedProperty rows = levelObject.FindProperty("rows");

        using (new EditorGUI.DisabledScope(rows.arraySize <= 1))
        {
            if (GUILayout.Button("Remove Last Row"))
            {
                RemoveLastRow();
            }
        }

        if (GUILayout.Button("Clear Map"))
        {
            ClearMap();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawShotSequence()
    {
        SerializedProperty shots = levelObject.FindProperty("shotColors");
        EditorGUILayout.LabelField("Shot Sequence", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        int requestedShotCount = EditorGUILayout.DelayedIntField("Shot Amount", shots.arraySize);

        if (EditorGUI.EndChangeCheck())
        {
            requestedShotCount = Mathf.Clamp(requestedShotCount, MinimumShotCount, MaximumShotCount);
            bool canResize = true;

            if (requestedShotCount < shots.arraySize)
            {
                canResize = EditorUtility.DisplayDialog(
                    "Reduce Shot Amount",
                    $"Remove the last {shots.arraySize - requestedShotCount} shot slots?",
                    "Remove",
                    "Cancel");
            }

            if (canResize && requestedShotCount != shots.arraySize)
            {
                ResizeShots(requestedShotCount);
                shots = levelObject.FindProperty("shotColors");
            }
        }

        EditorGUILayout.LabelField($"{shots.arraySize} designed shots", EditorStyles.miniLabel);

        int columns = 10;

        for (int index = 0; index < shots.arraySize; index += columns)
        {
            EditorGUILayout.BeginHorizontal();

            for (int offset = 0; offset < columns && index + offset < shots.arraySize; offset++)
            {
                int shotIndex = index + offset;
                SerializedProperty shot = shots.GetArrayElementAtIndex(shotIndex);
                BubbleColor color = (BubbleColor)shot.enumValueIndex;
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = GetDisplayColor(color);
                Rect rect = GUILayoutUtility.GetRect(36f, 34f, GUILayout.Width(36f), GUILayout.Height(34f));
                GUI.Box(rect, $"{shotIndex + 1}\n{GetLetter(color)}", EditorStyles.miniButton);
                GUI.backgroundColor = oldColor;
                HandleShotInput(rect, shot, shotIndex);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.LabelField("Choose a paint color, then click shot slots. Right click erases.", EditorStyles.miniLabel);
    }

    private void HandleShotInput(Rect rect, SerializedProperty shot, int shotIndex)
    {
        Event currentEvent = Event.current;

        if (!rect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        if (currentEvent.type != EventType.MouseDown && currentEvent.type != EventType.MouseDrag)
        {
            return;
        }

        int item = 100000 + shotIndex;

        if (item == lastPaintedItem)
        {
            return;
        }

        BubbleColor newColor = currentEvent.button == 1 ? BubbleColor.Empty : paintColor;
        SetEnumValue(shot, newColor, $"Paint Shot {shotIndex + 1}");
        lastPaintedItem = item;
        currentEvent.Use();
    }

    private void DrawValidation()
    {
        List<string> warnings = GetValidationWarnings();
        EditorGUILayout.LabelField("Level Check", EditorStyles.boldLabel);

        if (warnings.Count == 0)
        {
            EditorGUILayout.HelpBox("Level is ready.", MessageType.Info);
            return;
        }

        foreach (string warning in warnings)
        {
            EditorGUILayout.HelpBox(warning, MessageType.Warning);
        }
    }

    private List<string> GetValidationWarnings()
    {
        List<string> warnings = new List<string>();
        SerializedProperty rows = levelObject.FindProperty("rows");
        SerializedProperty shots = levelObject.FindProperty("shotColors");

        if (shots.arraySize < MinimumShotCount || shots.arraySize > MaximumShotCount)
        {
            warnings.Add($"Shot amount must be between {MinimumShotCount} and {MaximumShotCount}.");
        }

        int emptyShots = 0;
        HashSet<BubbleColor> shotColors = new HashSet<BubbleColor>();

        for (int index = 0; index < shots.arraySize; index++)
        {
            BubbleColor color = (BubbleColor)shots.GetArrayElementAtIndex(index).enumValueIndex;

            if (color == BubbleColor.Empty)
            {
                emptyShots++;
            }
            else
            {
                shotColors.Add(color);
            }
        }

        if (emptyShots > 0)
        {
            warnings.Add($"{emptyShots} shot slots are empty.");
        }

        HashSet<BubbleColor> mapColors = new HashSet<BubbleColor>();
        int unsupported = CountUnsupportedBubbles(mapColors);

        if (unsupported > 0)
        {
            warnings.Add($"{unsupported} bubbles are not connected to the top row and will fall immediately after a match check.");
        }

        foreach (BubbleColor color in shotColors)
        {
            if (!mapColors.Contains(color))
            {
                warnings.Add($"The shot sequence uses {color}, but the map has no {color} bubbles.");
            }
        }

        for (int row = 1; row < rows.arraySize; row += 2)
        {
            SerializedProperty cells = rows.GetArrayElementAtIndex(row).FindPropertyRelative("cells");

            if ((BubbleColor)cells.GetArrayElementAtIndex(level.Columns - 1).enumValueIndex != BubbleColor.Empty)
            {
                warnings.Add($"Row {row + 1} uses its unavailable final cell.");
            }
        }

        return warnings;
    }

    private int CountUnsupportedBubbles(HashSet<BubbleColor> mapColors)
    {
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        HashSet<Vector2Int> supported = new HashSet<Vector2Int>();
        Queue<Vector2Int> open = new Queue<Vector2Int>();

        for (int row = 0; row < level.RowCount; row++)
        {
            for (int column = 0; column < level.Columns; column++)
            {
                BubbleColor color = level.GetCell(row, column);

                if (color == BubbleColor.Empty)
                {
                    continue;
                }

                Vector2Int cell = new Vector2Int(column, row);
                occupied.Add(cell);
                mapColors.Add(color);

                if (row == 0)
                {
                    supported.Add(cell);
                    open.Enqueue(cell);
                }
            }
        }

        while (open.Count > 0)
        {
            Vector2Int cell = open.Dequeue();

            foreach (Vector2Int neighbour in GetNeighbours(cell))
            {
                if (occupied.Contains(neighbour) && supported.Add(neighbour))
                {
                    open.Enqueue(neighbour);
                }
            }
        }

        return occupied.Count - supported.Count;
    }

    private IEnumerable<Vector2Int> GetNeighbours(Vector2Int cell)
    {
        Vector2Int[] offsets = cell.y % 2 == 0
            ? new[]
            {
                new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(-1, -1),
                new Vector2Int(0, -1), new Vector2Int(-1, 1), new Vector2Int(0, 1)
            }
            : new[]
            {
                new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, -1),
                new Vector2Int(1, -1), new Vector2Int(0, 1), new Vector2Int(1, 1)
            };

        foreach (Vector2Int offset in offsets)
        {
            Vector2Int neighbour = cell + offset;

            if (neighbour.y >= 0 && neighbour.y < level.RowCount && neighbour.x >= 0 && neighbour.x < level.Columns)
            {
                if (neighbour.y % 2 == 0 || neighbour.x < level.Columns - 1)
                {
                    yield return neighbour;
                }
            }
        }
    }

    private void SetEnumValue(SerializedProperty property, BubbleColor color, string undoName)
    {
        if (property.enumValueIndex == (int)color)
        {
            return;
        }

        Undo.RecordObject(level, undoName);
        property.enumValueIndex = (int)color;
        SaveLevel();
    }

    private void AddRow()
    {
        SerializedProperty rows = levelObject.FindProperty("rows");
        Undo.RecordObject(level, "Add Bubble Row");
        int newRow = rows.arraySize;
        rows.arraySize++;
        SerializedProperty cells = rows.GetArrayElementAtIndex(newRow).FindPropertyRelative("cells");

        if (cells.arraySize != level.Columns)
        {
            cells.arraySize = level.Columns;
        }

        for (int column = 0; column < cells.arraySize; column++)
        {
            cells.GetArrayElementAtIndex(column).enumValueIndex = (int)BubbleColor.Empty;
        }

        SaveLevel();
    }

    private void RemoveLastRow()
    {
        SerializedProperty rows = levelObject.FindProperty("rows");
        Undo.RecordObject(level, "Remove Bubble Row");
        rows.DeleteArrayElementAtIndex(rows.arraySize - 1);
        SaveLevel();
    }

    private void ClearMap()
    {
        if (!EditorUtility.DisplayDialog("Clear Map", "Erase every bubble from this map?", "Clear", "Cancel"))
        {
            return;
        }

        Undo.RecordObject(level, "Clear Bubble Map");
        SerializedProperty rows = levelObject.FindProperty("rows");

        for (int row = 0; row < rows.arraySize; row++)
        {
            SerializedProperty cells = rows.GetArrayElementAtIndex(row).FindPropertyRelative("cells");

            for (int column = 0; column < cells.arraySize; column++)
            {
                cells.GetArrayElementAtIndex(column).enumValueIndex = (int)BubbleColor.Empty;
            }
        }

        SaveLevel();
    }

    private void ResizeShots(int shotCount)
    {
        Undo.RecordObject(level, "Resize Shot Sequence");
        SerializedProperty shots = levelObject.FindProperty("shotColors");
        int oldSize = shots.arraySize;
        shots.arraySize = shotCount;

        for (int index = oldSize; index < shotCount; index++)
        {
            shots.GetArrayElementAtIndex(index).enumValueIndex = (int)BubbleColor.Empty;
        }

        SaveLevel();
    }

    private void SaveLevel()
    {
        levelObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(level);
        AssetDatabase.SaveAssetIfDirty(level);
        levelObject.Update();
        RefreshPreview();
        Repaint();
    }

    private void SetLevel(LevelData chosenLevel)
    {
        level = chosenLevel;
        levelObject = level == null ? null : new SerializedObject(level);
        RefreshPreview();
        Repaint();
    }

    private void EnsureSerializedObject()
    {
        if (levelObject == null || levelObject.targetObject != level)
        {
            levelObject = new SerializedObject(level);
        }
    }

    private void TryUseSceneLevel()
    {
        MapLoader mapLoader = FindMapLoader();

        if (mapLoader != null && mapLoader.Level != null)
        {
            SetLevel(mapLoader.Level);
        }
    }

    private void AssignToMapLoader()
    {
        MapLoader mapLoader = FindMapLoader();

        if (mapLoader == null)
        {
            EditorUtility.DisplayDialog("MapLoader Missing", "Add a MapLoader to the open scene first.", "OK");
            return;
        }

        Undo.RecordObject(mapLoader, "Assign Bubble Level");
        SerializedObject mapObject = new SerializedObject(mapLoader);
        mapObject.FindProperty("level").objectReferenceValue = level;
        mapObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(mapLoader);
        EditorSceneManager.MarkSceneDirty(mapLoader.gameObject.scene);
        RefreshPreview();
    }

    private void DuplicateLevel()
    {
        string sourcePath = AssetDatabase.GetAssetPath(level);
        string folder = string.IsNullOrEmpty(sourcePath) ? "Assets" : System.IO.Path.GetDirectoryName(sourcePath)?.Replace("\\", "/");
        string path = EditorUtility.SaveFilePanelInProject("Duplicate Bubble Level", level.name + " Copy", "asset", "Choose where to save the copied level.", folder);

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        LevelData copy = Instantiate(level);
        copy.name = System.IO.Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(copy, path);
        AssetDatabase.SaveAssets();
        SetLevel(copy);
        Selection.activeObject = copy;
    }

    private void RefreshPreview()
    {
        RemovePreview();

        if (!livePreview || level == null || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SceneView.RepaintAll();
            return;
        }

        MapLoader mapLoader = FindMapLoader();

        if (mapLoader == null)
        {
            SceneView.RepaintAll();
            return;
        }

        SerializedObject mapObject = new SerializedObject(mapLoader);
        BubbleView prefab = mapObject.FindProperty("bubblePrefab").objectReferenceValue as BubbleView;

        if (prefab == null)
        {
            SceneView.RepaintAll();
            return;
        }

        float horizontalSpacing = mapObject.FindProperty("horizontalSpacing").floatValue;
        float verticalSpacing = mapObject.FindProperty("verticalSpacing").floatValue;
        float bubbleScale = mapObject.FindProperty("bubbleScale").floatValue;
        previewRoot = new GameObject(PreviewName);
        previewRoot.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor;
        previewRoot.transform.SetPositionAndRotation(mapLoader.transform.position, mapLoader.transform.rotation);
        previewRoot.transform.localScale = mapLoader.transform.lossyScale;

        for (int row = 0; row < level.RowCount; row++)
        {
            for (int column = 0; column < level.Columns; column++)
            {
                BubbleColor color = level.GetCell(row, column);

                if (color == BubbleColor.Empty || row % 2 != 0 && column == level.Columns - 1)
                {
                    continue;
                }

                GameObject previewBubble = PrefabUtility.InstantiatePrefab(prefab.gameObject, previewRoot.transform) as GameObject;

                if (previewBubble == null)
                {
                    continue;
                }

                previewBubble.name = $"Preview Bubble {row + 1}-{column + 1}";
                SetHideFlags(previewBubble);
                float centeredColumn = column - (level.Columns - 1) * 0.5f;
                float rowOffset = row % 2 == 0 ? 0f : horizontalSpacing * 0.5f;
                previewBubble.transform.localPosition = new Vector3(centeredColumn * horizontalSpacing + rowOffset, -row * verticalSpacing, 0f);
                previewBubble.transform.localRotation = Quaternion.identity;
                previewBubble.transform.localScale = prefab.transform.localScale * bubbleScale;
                previewBubble.GetComponent<BubbleView>().SetColor(color);
            }
        }

        SceneView.RepaintAll();
    }

    private void RemovePreview()
    {
        if (previewRoot != null)
        {
            DestroyImmediate(previewRoot);
            previewRoot = null;
        }
    }

    private static void SetHideFlags(GameObject gameObject)
    {
        gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor;

        foreach (Transform child in gameObject.transform)
        {
            SetHideFlags(child.gameObject);
        }
    }

    private void HandleUndoRedo()
    {
        if (level != null)
        {
            levelObject = new SerializedObject(level);
        }

        RefreshPreview();
        Repaint();
    }

    private void HandlePlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            RemovePreview();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            RefreshPreview();
        }
    }

    private void HandleMouseRelease()
    {
        Event currentEvent = Event.current;

        if (currentEvent.rawType == EventType.MouseUp)
        {
            lastPaintedItem = -1;
        }
    }

    private static BubbleView FindBubblePrefab()
    {
        MapLoader mapLoader = FindMapLoader();

        if (mapLoader == null)
        {
            return null;
        }

        SerializedObject mapObject = new SerializedObject(mapLoader);
        return mapObject.FindProperty("bubblePrefab").objectReferenceValue as BubbleView;
    }

    private static MapLoader FindMapLoader()
    {
        return Object.FindAnyObjectByType<MapLoader>(FindObjectsInactive.Include);
    }

    private static string GetLetter(BubbleColor color)
    {
        return color switch
        {
            BubbleColor.Red => "R",
            BubbleColor.Blue => "B",
            BubbleColor.Green => "G",
            BubbleColor.Yellow => "Y",
            _ => "E"
        };
    }

    private Color GetDisplayColor(BubbleColor color)
    {
        return color switch
        {
            BubbleColor.Red => redDisplayColor,
            BubbleColor.Blue => blueDisplayColor,
            BubbleColor.Green => greenDisplayColor,
            BubbleColor.Yellow => yellowDisplayColor,
            _ => new Color(0.65f, 0.65f, 0.65f)
        };
    }
}
