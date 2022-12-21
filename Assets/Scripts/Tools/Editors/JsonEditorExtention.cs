#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[CustomEditor(typeof(TextAsset), true)]
public class JsonEditorExtention : Editor
{
    //Variable
    private Action onSubView;

    private Vector2 inspectorPos;
    private Vector2 scrollPos;

    private static Event currentEvent;
    private static Rect initialRect;

    public static JObject jsonObject;
    private static JProperty propertyToRename;
    private static ConcurrentDictionary<string, bool> dicFold = new ConcurrentDictionary<string, bool>();

    public static string editText = "";
    private static string propertyRename;

    private static float limitSpace;

    public static bool jsonShow;
    public static bool treeShow;
    public static bool treeExpand;
    public static bool jsonTextEdit;
    public static bool jsonInspectorEdit;

    private static bool expanded;

    //Property
    public bool IsCompatible => EditorPath.EndsWith(".json");
    private string EditorPath => AssetDatabase.GetAssetPath(target);
    public static string Path;

    #region GUIStyle Variable
    private static GUIStyle btnStyle = null;
    private static GUIStyle smallBtnStyle = null;
    private static GUIStyle showStyle = null;
    private static GUIStyle labelStyle = null;
    private static GUIStyle filedStyle = null;
    private static GUIStyle editStyle = null;

    public static GUIStyle BtnStyle 
    {
        get{
            if(btnStyle == null)
            {
                btnStyle = new GUIStyle(EditorStyles.miniButton);
                btnStyle.alignment = TextAnchor.MiddleCenter;
                btnStyle.fontSize = 15;
                btnStyle.normal.textColor = Color.white;
                btnStyle.border = new RectOffset(5, 5, 5, 5);
                btnStyle.wordWrap = true;
                btnStyle.fixedWidth = WIDTH;
                btnStyle.fixedHeight = HEIGHT;
            }

            return btnStyle;
        }
    }

    public static GUIStyle SmallBtnStyle
    {
        get
        {
            if (smallBtnStyle == null)
            {
                smallBtnStyle = new GUIStyle(EditorStyles.miniButton);
                smallBtnStyle.alignment = TextAnchor.MiddleCenter | TextAnchor.MiddleLeft;
                smallBtnStyle.fontSize = 10;
                smallBtnStyle.normal.textColor = Color.white;
            }

            return smallBtnStyle;
        }
    }

    public static GUIStyle ShowStyle
    {
        get
        {
            if (showStyle == null)
            {
                showStyle = new GUIStyle(EditorStyles.helpBox);
                showStyle.alignment = TextAnchor.UpperLeft;
                showStyle.fontSize = 13;
                showStyle.normal.textColor = Color.white;
            }
            return showStyle;
        }
    }

    public static GUIStyle LabelStyle
    {
        get
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(EditorStyles.largeLabel);
                labelStyle.fontSize = 13;
            }
            return labelStyle;
        }
    }


    public static GUIStyle FiledStyle
    {
        get
        {
            if (filedStyle == null)
            {
                filedStyle = new GUIStyle(EditorStyles.textField);
                filedStyle.fontSize = 15;
                filedStyle.fixedHeight = 20;
            }
            return filedStyle;
        }
    }


    public static GUIStyle EditStyle
    {
        get
        {
            if (editStyle == null)
            {
                editStyle = new GUIStyle(EditorStyles.textArea);
                editStyle.fontSize = 13;
            }
            return editStyle;
        }
    }
    #endregion

    #region Constant 
    const string JSONFILE = "New Json File";
    const string JSONFORMAT = ".json";

    const int SHORT_SPACE = 2;
    const int NORMAL_SPACE = 5;
    const int BIG_SPACE = 10;
    const int HORIZONTAL_SPACE = 20;
    const int HUGE_SPACE = 30;
    const int LIMIT_SPACE = 800;

    const int WIDTH = 150; // !!!! Change button width size for each Unity version
    const int HEIGHT = 30; // !!!! Change button height size for each Unity version
    #endregion

    private void OnEnable()
    {
        if (IsCompatible)
        {
            jsonShow = true;
            treeShow = false;
            
            dicFold.Clear();
            Path = EditorPath;
            ReadJson();
        }

    }

    private void OnDisable()
    {
        dicFold.Clear();
    }

    public override void OnInspectorGUI()
    {
        if (IsCompatible)
        {
            GUI.enabled = true;
            BaseView();
        }
        base.OnInspectorGUI();

    }

    private void BaseView()
    {
        var lineCount = File.ReadLines(EditorPath).Count();
        initialRect = new Rect(10, EditorGUIUtility.singleLineHeight * 3 + 10, EditorGUIUtility.currentViewWidth - HUGE_SPACE, lineCount * 5 + 500);
        limitSpace = initialRect.size.y >= LIMIT_SPACE ? LIMIT_SPACE : initialRect.size.y;

        GUILayout.BeginVertical();
        GUILayout.BeginArea(new Rect(initialRect.x, initialRect.y, initialRect.size.x, initialRect.size.y));

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, new GUIStyle(EditorStyles.helpBox), GUILayout.Width(initialRect.width), GUILayout.Height(initialRect.size.y), GUILayout.MaxHeight(LIMIT_SPACE));
        if (jsonShow)
        {
            ShowMainView();
            onSubView = ShowSubView;
        }
        else if (jsonTextEdit)
        {
            TextEditMainView();
            onSubView = TextEditSubView;
        }
        else if (treeShow)
        {
            ShowTreeMainView();
            onSubView = TreeSubView;
        }
        else
        {
            ShowTreeMainView();
            onSubView = InspecterEditSubView;
        }
        EditorGUILayout.EndScrollView();

        GUILayout.EndArea();
        GUILayout.EndVertical();

        if(onSubView != null)
        {
            onSubView();
        }
    }
    #region Inpector View
    public static void ShowMainView()
    {
        if (jsonObject != null)
        {
            GUILayout.Label(jsonObject.ToString(), ShowStyle);
        }
    }
    private void ShowSubView()
    {
        // ====================================================================================================================================
        GUILayout.Space(limitSpace + BIG_SPACE);
        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth));
        if (GUILayout.Button("Edit Inspector", BtnStyle, GUILayout.Width(initialRect.size.x / 2 - NORMAL_SPACE), GUILayout.Height(HUGE_SPACE)))
        {
            jsonShow = false;
        }
        // ====================================================================================================================================
        if (GUILayout.Button("Edit Text", BtnStyle, GUILayout.Width(initialRect.size.x / 2 - NORMAL_SPACE), GUILayout.Height(HUGE_SPACE)))
        {
            if(jsonObject!=null)
                editText = jsonObject.ToString();

            jsonShow = false;
            jsonTextEdit = true;
        }
        EditorGUILayout.EndHorizontal();
    }

    public static void ShowTreeMainView()
    {
        if (jsonObject != null)
        {
            IEnumerable tokenAble = jsonObject.Values<JProperty>();
            IEnumerator tokenRator = tokenAble.GetEnumerator();
            while (tokenRator.MoveNext())
            {
                JProperty jProperty = tokenRator.Current as JProperty;

                if (treeShow)
                {
                    DrawTreeGUI(jProperty);
                }
                else
                {
                    DrawGUI(jProperty);
                }
            }
        }
    }

    private static void SubEditControlView()
    {
        // ====================================================================================================================================
        GUILayout.Space(limitSpace + BIG_SPACE);
        if (GUILayout.Button("Save", BtnStyle, GUILayout.Width(initialRect.size.x - NORMAL_SPACE), GUILayout.Height(HUGE_SPACE)))
        {
            jsonInspectorEdit = true;
            SaveJson(true);
            ReadJson();
        }
        // ====================================================================================================================================
        GUILayout.Space(NORMAL_SPACE);
        if (GUILayout.Button("Add new Property", BtnStyle, GUILayout.Width(initialRect.size.x - NORMAL_SPACE), GUILayout.Height(HUGE_SPACE)))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddSeparator("");

            JContainer jContainer = jsonObject.Value<JContainer>();

            menu.AddItem(new GUIContent("String"), false, () =>
            {
                AddNewProperty<string>(jContainer);
            });
            menu.AddItem(new GUIContent("Single"), false, () =>
            {
                AddNewProperty<float>(jContainer);
            });
            menu.AddItem(new GUIContent("Integer"), false, () =>
            {
                AddNewProperty<int>(jContainer);
            });
            menu.AddItem(new GUIContent("Boolean"), false, () =>
            {
                AddNewProperty<bool>(jContainer);
            });
            menu.AddItem(new GUIContent("Object"), false, () =>
            {
                AddNewProperty<JObject>(jContainer, JTokenType.Object);
            });

            menu.AddItem(new GUIContent("Array"), false, () =>
            {
                AddNewProperty<JArray>(jContainer, JTokenType.Array);
            });

            currentEvent = Event.current;
            menu.DropDown(new Rect(currentEvent.mousePosition.x, currentEvent.mousePosition.y, 10, 10));

        }
    }

    private static void SubEditShowView()
    {
        // ====================================================================================================================================
        GUILayout.Space(NORMAL_SPACE);
        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth));
        if (GUILayout.Button("Show Json", BtnStyle, GUILayout.Width(initialRect.size.x / 2 - NORMAL_SPACE), GUILayout.Height(HUGE_SPACE)))
        {
            jsonShow = true;
            ReadJson();
        }

        // ====================================================================================================================================
        if (GUILayout.Button("Show Tree", BtnStyle, GUILayout.Width(initialRect.size.x / 2 - NORMAL_SPACE), GUILayout.Height(HUGE_SPACE)))
        {
            treeShow = true;
        }

        EditorGUILayout.EndHorizontal();
    }

    public static void InspecterEditSubView(bool tool = false)
    {
        if (tool || initialRect == Rect.zero)
        {
            var lineCount = File.ReadLines(Path).Count();
            initialRect.size = new Vector2(200, lineCount * 5 + 500);
        }

        SubEditControlView();
    }

    private static void InspecterEditSubView()
    {
        SubEditControlView();
        SubEditShowView();
    }

    private void TreeSubView()
    {
        // ====================================================================================================================================
        GUILayout.Space(limitSpace + BIG_SPACE);
        treeExpand = GUILayout.Toggle(treeExpand, "Tree Expand");
        FoldExpand(treeExpand);

        // ====================================================================================================================================
        GUILayout.Space(NORMAL_SPACE);
        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth));
        if (GUILayout.Button("Edit Inspector", BtnStyle, GUILayout.Width(initialRect.size.x / 2 - NORMAL_SPACE), GUILayout.Height(HUGE_SPACE)))
        {
            treeShow = false;
        }
        // ====================================================================================================================================
        if (GUILayout.Button("Edit Text", BtnStyle, GUILayout.Width(initialRect.size.x / 2 - NORMAL_SPACE), GUILayout.Height(HUGE_SPACE)))
        {
            if (jsonObject != null)
                editText = jsonObject.ToString();

            jsonShow = false;
            jsonTextEdit = true;
        }
        EditorGUILayout.EndHorizontal();

        // ====================================================================================================================================
        GUILayout.Space(NORMAL_SPACE);
        if (GUILayout.Button("Show Json", BtnStyle, GUILayout.Width(initialRect.size.x), GUILayout.Height(HUGE_SPACE)))
        {
            treeShow = false;
            jsonShow = true;
            ReadJson();
        }
    }

    public static void TextEditMainView()
    {
        editText = GUILayout.TextArea(editText, EditStyle);
    }

    private void TextEditSubView()
    {
        // ====================================================================================================================================
        GUILayout.Space(limitSpace + BIG_SPACE);
        if (GUILayout.Button("Save", BtnStyle, GUILayout.Width(initialRect.size.x), GUILayout.Height(HUGE_SPACE)))
        {
            SaveJson();
            ReadJson();
        }
        // ====================================================================================================================================
        GUILayout.Space(NORMAL_SPACE);
        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth));

        if (GUILayout.Button("Show Json", BtnStyle, GUILayout.Width(initialRect.size.x / 2 - NORMAL_SPACE), GUILayout.Height(HUGE_SPACE)))
        {
            editText = "";
            jsonShow = true;
            jsonTextEdit = false;
        }
        // ====================================================================================================================================
        if (GUILayout.Button("Show Tree", BtnStyle, GUILayout.Width(initialRect.size.x / 2 - NORMAL_SPACE), GUILayout.Height(HUGE_SPACE)))
        {
            editText = "";
            treeShow = true;
            jsonTextEdit = false;
        }
        EditorGUILayout.EndHorizontal();
    }
    #endregion

    #region Button Event
    private static void SaveJson(bool change = false)
    {
        if (jsonObject != null)
        {
            if (jsonInspectorEdit || jsonTextEdit)
            {
                if (File.Exists(Path))
                {
                    try
                    {
                        WriteJson(change);
                        Debug.Log("Json Edit Success : " + Path);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Json Edit Fail : " + Path);
                        Debug.LogException(e);
                    }
                }
            }

        }
    }
    private static void AddNewProperty<T>(JContainer jContainer, JTokenType type = JTokenType.None)
    {
        string typeName = typeof(T).Name.ToLower();
        object value = default(T);
        switch (Type.GetTypeCode(typeof(T)))
        {
            case TypeCode.Boolean:
                break;
            case TypeCode.Int32:
                typeName = "integer";
                break;
            case TypeCode.Single:
                typeName = "float";
                break;
            case TypeCode.String:
                value = "";
                break;
            default:
                 if (typeof(T) == typeof(JArray))
                {
                    typeName = "Empty Array";
                    value = new JArray();
                }
                else
                {
                    typeName = "Empty Object";
                    value = new JObject();
                }
                break;
        }
        jsonInspectorEdit = true;

        JProperty property;
        JObject jObject = jContainer as JObject;

        string name = GetUniqueName(jContainer as JObject, string.Format("new {0}", typeName));
        property = new JProperty(name, value);

        if (type == JTokenType.Array)
        {
            JArray jArray = jContainer as JArray;
            if (jArray == null)
            {
                //jArray = new JArray();
                jContainer.Add(property);
            }
            else
            {
                JObject dataObject = new JObject(property);
                jContainer.Add(dataObject);
            }
        }
        else
        {
            jContainer.Add(property);
        }
    }

    #endregion

    #region File Write & Load
    private static void ReadJson()
    {
        if (!string.IsNullOrWhiteSpace(Path) && File.Exists(Path))
        {
            editText = File.ReadAllText(Path);
            jsonObject = JsonConvert.DeserializeObject<JObject>(editText);
            if (jsonObject == null) jsonObject = new JObject();
        }
    }

    public static void ReadJson(string selectPath)
    {
        if (!string.IsNullOrWhiteSpace(selectPath) && File.Exists(selectPath))
        {
            editText = File.ReadAllText(selectPath);
            jsonObject = JsonConvert.DeserializeObject<JObject>(editText);
            if (jsonObject == null) jsonObject = new JObject();
        }
    }

    private TextAsset LoadJson()
    {
        return AssetDatabase.LoadAssetAtPath<TextAsset>(Path);
    }

    private static void WriteJson(bool change = false)
    {
        if (change){
            editText = jsonObject.ToString();
        }

        jsonInspectorEdit = false;
        jsonTextEdit = false;

        File.WriteAllText(Path, editText);
        AssetDatabase.Refresh();
    }

    public static void DeleteFile(string seletPath)
    {
        if (!string.IsNullOrWhiteSpace(seletPath) && File.Exists(seletPath))
        {
            Debug.Log("File Delete : " + seletPath);
            File.Delete(seletPath);
            
        }
    }
    #endregion

    #region Json Parsing
    private static void ChildJToken(JToken token)
    {
        GUILayout.BeginVertical();
        for (int i = 0; i < token.Count<JToken>(); i++)
        {
            JProperty jProperty = token.ElementAt<JToken>(i).Value<JProperty>();

            if (treeShow)
            {
                DrawTreeGUI(jProperty);
            }
            else
            {
                DrawGUI(jProperty);
            }
        }
        GUILayout.EndVertical();
    }

    private static void ArrayJToken(JToken token)
    {
        GUILayout.BeginVertical();
        JToken[] tokenArray = token.ToArray<JToken>();
        for (int i = 0; i < tokenArray.Length; i++)
        {
            if (treeShow)
            {
                DrawTreeGUI(tokenArray[i]);
            }
            else
            {
                DrawGUI(tokenArray[i]);
            }
        }
        GUILayout.EndVertical();
    }
    #endregion

    #region DrawGUI
    public static void DrawGUI(JProperty jProperty)
    {
        GUILayout.Space(SHORT_SPACE);
        float propertyNameWidth = GUI.skin.label.CalcSize(new GUIContent(jProperty.Name.ToString())).x * 1.25f + 10;

        GUILayout.BeginHorizontal();

        CustomButton(jProperty);

        switch (jProperty.Value.Type)
        {
            case JTokenType.Boolean:
                LabelOrField(jProperty, propertyNameWidth);

                bool boolValue = jProperty.Value.ToObject<bool>();
                boolValue = GUILayout.Toggle(boolValue,"");
                jProperty.Value = boolValue;
                break;
            case JTokenType.Integer:
                LabelOrField(jProperty, propertyNameWidth);

                int intValue = jProperty.Value.ToObject<int>();
                intValue = EditorGUILayout.IntField(intValue, FiledStyle);
                jProperty.Value = intValue;
                break;
            case JTokenType.Float:
                LabelOrField(jProperty, propertyNameWidth);

                float floatValue = jProperty.Value.ToObject<float>();
                floatValue = EditorGUILayout.FloatField(floatValue, FiledStyle);
                jProperty.Value = floatValue;
                break;
            case JTokenType.String:
                LabelOrField(jProperty, propertyNameWidth);

                string stringValue = jProperty.Value.ToObject<String>();
                stringValue = GUILayout.TextField(stringValue, FiledStyle);
                jProperty.Value = stringValue;
                break;
            case JTokenType.Property:
                Debug.LogWarning(jProperty.Value + " Didn't Parsing");
                break;
            case JTokenType.Object:
                LabelOrField(jProperty,  propertyNameWidth);
                ChildJToken(jProperty.Value);
                break;
            case JTokenType.Array:
                //Debug.Log(jProperty);
                LabelOrField(jProperty, propertyNameWidth);
                ArrayJToken(jProperty.Value);
                break;
            case JTokenType.None:
            case JTokenType.Null:
                Debug.LogWarning("Json Parsing Warning : #" + jProperty.Name + " - " + jProperty.Type + " Value  Null or None - Have to set Default Value");
                break;
            default:
                Debug.LogWarning("Json Parsing Warning : #" + jProperty.Name + " - " + jProperty.Type + " Not Using Data Type!!");
                break;
        }
        GUILayout.EndHorizontal();
    }

    public static void DrawGUI(JToken jToken)
    {
        GUILayout.Space(SHORT_SPACE);
        if (jToken.First == null)
        {
            Debug.Log("null이닷");
        }
        float propertyNameWidth = GUI.skin.label.CalcSize(new GUIContent(jToken.First.ToString())).x * 1.25f + 10;

        GUILayout.BeginHorizontal();
        switch (jToken.Type)
        {
            case JTokenType.Object:
                ChildJToken(jToken);
                break;
            case JTokenType.Array:
                //Debug.Log(jToken.First);
                ArrayJToken(jToken);
                break;
            case JTokenType.None:
            case JTokenType.Null:
                Debug.LogWarning("Json Parsing Warning : #" + jToken.First + " - " + jToken.Type + " Value  Null or None - Have to set Default Value");
                break;
            default:
                Debug.LogWarning("Json Parsing Warning : #" + jToken.First + " - " + jToken.Type + "Check Data Type!!");
                break;
        }
        GUILayout.EndHorizontal();
    }
    #endregion
    
    #region TreeDrawGUI
    public static void DrawTreeGUI(JProperty jProperty)
    {
        GUILayout.Space(SHORT_SPACE);
        float propertyNameWidth = (GUI.skin.label.CalcSize(new GUIContent(jProperty.Name.ToString())).x + EditorGUI.indentLevel) * 1.25f + 10 ;
        bool fold = false;
        string key;

        switch (jProperty.Value.Type)
        {
            case JTokenType.Boolean:
            case JTokenType.Integer:
            case JTokenType.Float:
            case JTokenType.String:
                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * HORIZONTAL_SPACE);

                GUILayout.Label(jProperty.Name + " : ", LabelStyle, GUILayout.Width(propertyNameWidth));
                GUILayout.Label(jProperty.Value.ToString(), LabelStyle);

                GUILayout.EndHorizontal();
                break;
            case JTokenType.Property:
                Debug.LogWarning(jProperty.Value + " Didn't Parsing");
                break;
            case JTokenType.Object:
                EditorGUI.indentLevel++;

                string childCount = String.Format("[{0}]", jProperty.Value.Count<JToken>());
                key = jProperty.FoldKeyPath();

                fold = Fold(key, fold, true); // Default Value 저장 & 값 가져오기
                fold = EditorGUILayout.Foldout(fold, jProperty.Name + childCount);
                fold = Fold(key, fold); // Check Value 값 저장 & 값 가져오기

                if (fold)
                {
                    ChildJToken(jProperty.Value);
                }
                EditorGUI.indentLevel--;               
                break;
            case JTokenType.Array:
                EditorGUI.indentLevel++;
                string arrayCount = String.Format("[{0}]", jProperty.Value.Count<JToken>());
                key = jProperty.FoldKeyPath();

                fold = Fold(key, fold, true);
                fold = EditorGUILayout.Foldout(fold, jProperty.Name + arrayCount);
                fold = Fold(key, fold);

                if (fold)
                {
                    ArrayJToken(jProperty.Value);
                }
                EditorGUI.indentLevel--;
                break;
            case JTokenType.None:
            case JTokenType.Null:
                Debug.LogWarning("Json Parsing Warning : #" + jProperty.Name + " - " + jProperty.Type + " Value  Null or None - Have to set Default Value");
                break;
            default:
                Debug.LogWarning("Json Parsing Warning : #" + jProperty.Name + " - " + jProperty.Type + " Not Using Data Type!!");
                break;

        }
    }

    public static void DrawTreeGUI(JToken jToken)
    {
        GUILayout.Space(SHORT_SPACE);
        float propertyNameWidth = (GUI.skin.label.CalcSize(new GUIContent(jToken.First.ToString())).x + EditorGUI.indentLevel) * 1.25f + 10;
        //Debug.Log(jToken.FoldKeyPath());
        switch (jToken.Type)
        {
            case JTokenType.Object:
                ChildJToken(jToken);
                break;
            case JTokenType.Array:
                ArrayJToken(jToken);
                break;
            case JTokenType.None:
            case JTokenType.Null:
                Debug.LogWarning("Json Parsing Warning : #" + jToken.First + " - " + jToken.Type + " Value  Null or None - Have to set Default Value");
                break;
            default:
                Debug.LogWarning("Json Parsing Warning : #" + jToken.First + " - " + jToken.Type + " Check Data Type!!");
                break;
        }
    }
    #endregion

    #region Check GUI
    private static void LabelOrField(JProperty jProperty, float width)
    {
        if(propertyToRename != jProperty)
        {
            GUILayout.Label(jProperty.Name + " : ", LabelStyle, GUILayout.Width(width));
        }
        else
        {
            propertyRename = GUILayout.TextField(propertyRename, FiledStyle);
        }
    }

    private void LabelOrField(JToken jToken, float width)
    {
        if (propertyToRename != jToken.Parent)
        {
            GUILayout.Label(jToken.First + " : ", LabelStyle, GUILayout.Width(width));
        }
        else
        {
            propertyRename = GUILayout.TextField(propertyRename, FiledStyle);
        }
    }

    private static void CustomButton(JProperty jproperty)
    {
        string propertyName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jproperty.Name.ToLower()) + ":";
        float propertyNameWidth = GUI.skin.label.CalcSize(new GUIContent(propertyName)).x;

        if (propertyToRename != jproperty)
        {
            if (GUILayout.Button("►", SmallBtnStyle, GUILayout.Width(20), GUILayout.Height(20)))
            {
                GenericMenu menu = new GenericMenu();
                if (jproperty.Value.Type == JTokenType.Object || jproperty.Value.Type == JTokenType.Array)
                {
                    JTokenType type = jproperty.Value.Type == JTokenType.Object? JTokenType.Object : JTokenType.Array;

                    JContainer jContainer = jproperty.Value.Value<JContainer>();
                    //JObject jObject = jproperty.Value.Value<JObject>();

                    menu.AddSeparator("Add/");
                    menu.AddItem(new GUIContent("Add/String"), false, () =>
                    {
                        AddNewProperty<string>(jContainer, type);
                    });
                    menu.AddItem(new GUIContent("Add/float"), false, () =>
                    {
                        AddNewProperty<float>(jContainer, type);
                    });
                    menu.AddItem(new GUIContent("Add/Integer"), false, () =>
                    {
                        AddNewProperty<int>(jContainer, type);
                    });
                    menu.AddItem(new GUIContent("Add/Boolean"), false, () =>
                    {
                        AddNewProperty<bool>(jContainer, type);
                    });

                    menu.AddItem(new GUIContent("Add/ Object"), false, () =>
                    {
                        AddNewProperty<JObject>(jContainer, type);
                    });

                    menu.AddItem(new GUIContent("Add/Array"), false, () =>
                    {
                        AddNewProperty<JArray>(jContainer, type);
                    });

                }
                menu.AddItem(new GUIContent("Remove"), false, () => {
                    var grandParent = jproperty.Parent.Parent;
                    if(grandParent != null)
                    {
                        if(grandParent.Type == JTokenType.Array)
                        {
                            JArray array = grandParent as JArray;
                            array.Remove(jproperty.Parent);
                            return;
                        }
                    }
                    jproperty.Remove();
                });

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Rename"), false, () => {
                    propertyToRename = jproperty;
                    propertyRename = propertyToRename.Name;
                });
                currentEvent = Event.current;
                menu.DropDown(new Rect(currentEvent.mousePosition.x, currentEvent.mousePosition.y, 10, 10));
            }
        }
        else
        {
            GUI.color = new Color32(55, 255, 88, 255);
            if (GUILayout.Button("✔", SmallBtnStyle, GUILayout.Width(20), GUILayout.Height(20)))
            {
                JToken newToken = new JProperty(propertyRename, jproperty.Value);
                jproperty.Replace(newToken);
                EditorGUIUtility.ExitGUI();
            }
            GUI.color = Color.white;
        }
    }
    #endregion

    #region Json File Create
    [MenuItem("Assets/Create/JSON File", priority = 81)]
    public static void CreateNewJsonFile()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets/Data/Json/";
        }
        else if (System.IO.Path.GetExtension(path) != "")
        {
            path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }

        string countStr = "";
        int count = 0;

        while (true)
        {
            if (File.Exists(System.IO.Path.Combine(path, JSONFILE + countStr + JSONFORMAT)))
            {
                countStr = string.Format("({0})", ++count);
            }
            else
            {
                break;
            }
        }

        path = System.IO.Path.Combine(path, JSONFILE + countStr + JSONFORMAT);
        File.WriteAllText(path, new TextAsset().text);

        AssetDatabase.Refresh();
    }
    #endregion

    #region DrarwCusomProperty
    public static bool Fold(string key , bool fold, bool init = false)
    {
        if (!dicFold.ContainsKey(key))
        {
            dicFold.TryAdd(key, fold);
        }
        else
        {
            if (!init)
            {
                dicFold[key] = fold | treeExpand;
            }
        }
        return dicFold[key];
    }

    public static void FoldExpand(bool check)
    {
        if (expanded != check)
        {
            IEnumerator<string> itor = dicFold.Keys.GetEnumerator();

            while (itor.MoveNext())
            {
                string key = itor.Current;
                dicFold[key] = check;
            }
            expanded = check;
        }
    }

    #endregion


    private static string GetUniqueName(JObject jObject, string orignalName)
    {
        string uniqueName = orignalName;
        if (jObject != null)
        {
            int suffix = 0;
            while (jObject[uniqueName] != null && suffix < 100)
            {
                suffix++;
                if (suffix >= 100)
                {
                    Debug.LogError("Stop calling all your fields the same thing!");
                }
                uniqueName = string.Format("{0} {1}", orignalName, suffix.ToString());
            }
        }
        return uniqueName;
    }
}
#endif


public static class ExtensionMetod
{
    public static string FoldKeyPath(this JProperty jProperty)
    {
        JContainer parent = jProperty.Parent;
        StringBuilder sb = new StringBuilder();
        if (parent != null)
        {
            sb.Append(parent.Path);
            parent = parent.Parent;

        }
        sb.Append(jProperty.Name);
        return sb.ToString();
    }
}
