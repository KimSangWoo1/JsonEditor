#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class JsonEditor : EditorWindow
{
    //Variable
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;

    private string selectPath;
    private string selectName;

    private bool treeShow;
    private bool treeExpand;

    //Constant
    private const string PATH = "Assets/Data/Json/";

    #region GUIStyle Variable
    private GUIStyle titleStyle = null;

    //Property
    public GUIStyle TitleStyle
    {
        get
        {
            if (titleStyle == null)
            {
                titleStyle = EditorStyles.boldLabel;
                titleStyle.fontSize = 50; //EditorStyles.boldFont.fontSize
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.border = new RectOffset(1, 1, 1, 1);
                titleStyle.alignment = TextAnchor.MiddleCenter;
                titleStyle.normal.textColor = Color.black;
                titleStyle.normal.background = Texture2D.whiteTexture;
            }
            return titleStyle;
        }
    }

    #endregion

    //Const
    const string TITLE = "JSON EDITOR";

    private void OnEnable()
    {
        selectPath = null;
        selectName = null;

        treeShow = false;
        treeExpand = false;

        JsonEditorExtention.jsonShow = true;
    }

    [MenuItem("Tools/Json Tool")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(JsonEditor));
    }

    void OnGUI()
    {
        DefaultTopView();

        BaseMainView();
        BaseSubView();
    }

    private void DefaultTopView()
    {
        GUILayout.BeginHorizontal();
        if(GUILayout.Button("JSon 파일 보기", JsonEditorExtention.BtnStyle, GUILayout.Width(100), GUILayout.Height(30), GUILayout.ExpandWidth(true)))
        {
            JsonEditorExtention.jsonShow = true;
            JsonEditorExtention.treeShow = false;
            JsonEditorExtention.treeExpand = false;
            JsonEditorExtention.jsonTextEdit = false;
            JsonEditorExtention.jsonInspectorEdit = false;
        }
        if(GUILayout.Button("Json 작업", JsonEditorExtention.BtnStyle, GUILayout.Width(100), GUILayout.Height(30), GUILayout.ExpandWidth(true)))
        {
            JsonEditorExtention.jsonShow = false;
            JsonEditorExtention.treeShow = false;
            JsonEditorExtention.treeExpand = false;
            JsonEditorExtention.jsonTextEdit = true;
            JsonEditorExtention.jsonInspectorEdit = false;

            if (selectPath!=null)
                JsonEditorExtention.ReadJson(selectPath);
        }
        if(GUILayout.Button("설정", JsonEditorExtention.BtnStyle, GUILayout.Width(100), GUILayout.Height(30), GUILayout.ExpandWidth(true)))
        {
   
        }
        GUILayout.EndHorizontal();

        GUILayout.Box(GUIContent.none, GUILayout.ExpandWidth(true), GUILayout.Height(2.5f));
    }

    private void BaseMainView()
    {
        // ====================================================================================================================================
        GUILayout.Label(TITLE, TitleStyle, GUILayout.Width(100), GUILayout.Height(50), GUILayout.ExpandWidth(true));

        // ====================================================================================================================================

        GUILayout.BeginHorizontal(GUILayout.MinHeight(500));
        {
            GUILayout.BeginVertical(GUILayout.MinWidth(250));
            {
                GUILayout.Box("Json File List", JsonEditorExtention.ShowStyle);

                leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos, new GUIStyle(GUI.skin.box), GUILayout.ExpandWidth(true));

                JsonFiles();

                EditorGUILayout.EndScrollView();

            }
            GUILayout.EndVertical();

            // ====================================================================================================================================
            GUILayout.Space(10);
            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos, new GUIStyle(GUI.skin.box), GUILayout.ExpandWidth(true));

            if (JsonEditorExtention.jsonShow)
            {
                JsonEditorExtention.ShowMainView();
            }
            else if (JsonEditorExtention.treeShow)
            {
                JsonEditorExtention.ShowTreeMainView();
            }
            else if (JsonEditorExtention.jsonTextEdit)
            {
                JsonEditorExtention.TextEditMainView();
            }
            else
            {
                JsonEditorExtention.ShowTreeMainView(); // Edit Inspector
            }
            EditorGUILayout.EndScrollView();
        }
        GUILayout.EndHorizontal();


    }

    private void BaseSubView()
    {
        GUILayout.BeginHorizontal();

        if (JsonEditorExtention.jsonShow || JsonEditorExtention.treeShow)
        {
            // ====================================================================================================================================
            if (GUILayout.Button("파일 생성", JsonEditorExtention.BtnStyle, GUILayout.Width(100), GUILayout.Height(50)))
            {
                JsonEditorExtention.CreateNewJsonFile();
            }
            if (GUILayout.Button("파일 삭제", JsonEditorExtention.BtnStyle, GUILayout.Width(100), GUILayout.Height(50)))
            {
                JsonEditorExtention.DeleteFile(selectPath);
            }

            // ====================================================================================================================================
            GUILayout.Space(50);
            treeShow = GUILayout.Toggle(treeShow, "Tree View");
            if (treeShow)
            {
                JsonEditorExtention.jsonShow = false;
                JsonEditorExtention.treeShow = true;

                treeExpand = GUILayout.Toggle(treeExpand, "Tree Expand");
                JsonEditorExtention.FoldExpand(treeExpand);
            }
            else
            {
                JsonEditorExtention.jsonShow = true;
                JsonEditorExtention.treeShow = false;
            }
        }
        else
        {
            // ====================================================================================================================================
            if (GUILayout.Button("Text Edit", JsonEditorExtention.BtnStyle, GUILayout.Width(100), GUILayout.Height(50)))
            {
                JsonEditorExtention.jsonTextEdit = true;
                JsonEditorExtention.jsonInspectorEdit = false;
            }
            if (GUILayout.Button("Inspector Edit", JsonEditorExtention.BtnStyle, GUILayout.Width(100), GUILayout.Height(50)))
            {
                JsonEditorExtention.jsonTextEdit = false;
                JsonEditorExtention.jsonInspectorEdit = true;
            }

            if (JsonEditorExtention.jsonInspectorEdit)
            {
                GUILayout.Space(50);
                JsonEditorExtention.InspecterEditSubView(true);
            }
        }
        GUILayout.EndHorizontal();
    }

    private void EditorSettingView()
    {

    }

    private void JsonFiles()
    {
        DirectoryInfo info = new DirectoryInfo(PATH);
        FileInfo[] fileInfo = info.GetFiles("*.json");

        GUIStyle fileBox = new GUIStyle(EditorStyles.helpBox);
        fileBox.fontSize = 12;
        fileBox.fontStyle = FontStyle.Bold;
        fileBox.richText = true;

        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.fontSize = 12;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.richText = true;

        for (int i = 0; i < fileInfo.Length; i++)
        {
            GUILayout.BeginHorizontal("TextArea", GUILayout.MinHeight(24f));
            {
                GUILayout.Label((i + 1).ToString(), GUILayout.Width(30f));

                if (selectPath != null && selectName !=null)
                {
                    if (selectName.Equals(fileInfo[i].Name))
                    {
                        labelStyle.normal.background = Texture2D.whiteTexture;
                        labelStyle.normal.textColor = Color.red;
                     }
                    else
                    {
                        labelStyle.normal.background = Texture2D.blackTexture;
                        labelStyle.normal.textColor = Color.white;
                    }
                }

                if (GUILayout.Button(fileInfo[i].Name, labelStyle) == true)
                {
                    selectPath = PATH + fileInfo[i].Name;
                    selectName = fileInfo[i].Name;

                    JsonEditorExtention.ReadJson(selectPath);
                    JsonEditorExtention.Path = selectPath;
                    Debug.Log("Select : "+fileInfo[i].Name);

                }
            }  
            GUILayout.EndHorizontal();
        }
    }

}
#endif