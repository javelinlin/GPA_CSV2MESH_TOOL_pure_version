// jave.lin : 2022/09/27
// 尝试将 GPA 导出的 XBV 导出的 CSV，再次导出成 FBX
// Requirments : Unity FBX Export Packages
// Output Pipeline : Unity BRP

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;

public class JaveLin_GPA_CSV2FBX : EditorWindow
{
    [MenuItem("Tools/JaveLin_GPA_CSV2FBX...")]
    private static void _Show()
    {
        var win = EditorWindow.GetWindow<JaveLin_GPA_CSV2FBX>();
        win.titleContent = new GUIContent("JaveLin_GPA_CSV2FBX");
        win.Show();
    }

    private static void _Reset()
    {
        var win = EditorWindow.GetWindow<JaveLin_GPA_CSV2FBX>();
        win.ResetData();
    }

    // jave.lin : 顶点索引信息
    public class VertexIDInfo
    {
        public int IDX;                 // 索引
        public VertexInfo vertexInfo;   // 顶点信息
    }

    // jave.lin : 语义的类型
    public enum SemanticType
    {
        Unknow,

        VTX,

        IDX,

        POSITION_X,
        POSITION_Y,
        POSITION_Z,
        POSITION_W,

        NORMAL_X,
        NORMAL_Y,
        NORMAL_Z,
        NORMAL_W,

        TANGENT_X,
        TANGENT_Y,
        TANGENT_Z,
        TANGENT_W,

        TEXCOORD0_X,
        TEXCOORD0_Y,
        TEXCOORD0_Z,
        TEXCOORD0_W,

        TEXCOORD1_X,
        TEXCOORD1_Y,
        TEXCOORD1_Z,
        TEXCOORD1_W,

        TEXCOORD2_X,
        TEXCOORD2_Y,
        TEXCOORD2_Z,
        TEXCOORD2_W,

        TEXCOORD3_X,
        TEXCOORD3_Y,
        TEXCOORD3_Z,
        TEXCOORD3_W,

        TEXCOORD4_X,
        TEXCOORD4_Y,
        TEXCOORD4_Z,
        TEXCOORD4_W,

        TEXCOORD5_X,
        TEXCOORD5_Y,
        TEXCOORD5_Z,
        TEXCOORD5_W,

        TEXCOORD6_X,
        TEXCOORD6_Y,
        TEXCOORD6_Z,
        TEXCOORD6_W,

        TEXCOORD7_X,
        TEXCOORD7_Y,
        TEXCOORD7_Z,
        TEXCOORD7_W,

        COLOR0_X,
        COLOR0_Y,
        COLOR0_Z,
        COLOR0_W,
    }

    // jave.lin : 是否需要 根据 is from dx csv 标记来翻转 uv y 数据
    public enum Flip_UV_YCoordsType
    {
        TEXCOORD0_Y = SemanticType.TEXCOORD0_Y,
        TEXCOORD1_Y = SemanticType.TEXCOORD1_Y,
        TEXCOORD2_Y = SemanticType.TEXCOORD2_Y,
        TEXCOORD3_Y = SemanticType.TEXCOORD3_Y,
        TEXCOORD4_Y = SemanticType.TEXCOORD4_Y,
        TEXCOORD5_Y = SemanticType.TEXCOORD5_Y,
        TEXCOORD6_Y = SemanticType.TEXCOORD6_Y,
        TEXCOORD7_Y = SemanticType.TEXCOORD7_Y,
    }

    // jave.lin : 材质设置的方式
    public enum MaterialSetType
    {
        CreateNew,
        UsingExsitMaterialAsset,
    }

    // jave.lin : VBV 数据中的 normalized 类型
    [Flags]
    public enum VBVChannelNormalizedType
    {
        None,
        Nor_U8Bit,
        Nor_8Bit,
        Nor_U16Bit,
        Nor_16Bit,
        Nor_U32Bit,
        Nor_32Bit,
    }

    // jave.lin : 导出文件类型
    public enum ExportFileType
    {
        FBX,            // 注意如果使用这种方式的话，会有 uv 不能保存超过 vector2 分量的数据，也就是说，uv无法保存 vector3 或 vector4 的数据
        UnityMesh,      // 如果抓帧的模型发现 shaderlab 里面有使用到 vertex input TEXCOORD[n] 是超过2个以上的分量的，就不能使用 FBX 了，使用 UnityMesh 保存的网格可以保存下这些 uv 数据
    }

    // jave.lin : application to vertex shader 的通用类型（辅助转换用）
    public class VertexInfo
    {
        public int VTX;
        public int IDX;

        public float POSITION_X;
        public float POSITION_Y;
        public float POSITION_Z;
        public float POSITION_W;

        public float NORMAL_X;
        public float NORMAL_Y;
        public float NORMAL_Z;
        public float NORMAL_W;

        public float TANGENT_X;
        public float TANGENT_Y;
        public float TANGENT_Z;
        public float TANGENT_W;

        public float TEXCOORD0_X;
        public float TEXCOORD0_Y;
        public float TEXCOORD0_Z;
        public float TEXCOORD0_W;

        public float TEXCOORD1_X;
        public float TEXCOORD1_Y;
        public float TEXCOORD1_Z;
        public float TEXCOORD1_W;

        public float TEXCOORD2_X;
        public float TEXCOORD2_Y;
        public float TEXCOORD2_Z;
        public float TEXCOORD2_W;

        public float TEXCOORD3_X;
        public float TEXCOORD3_Y;
        public float TEXCOORD3_Z;
        public float TEXCOORD3_W;

        public float TEXCOORD4_X;
        public float TEXCOORD4_Y;
        public float TEXCOORD4_Z;
        public float TEXCOORD4_W;

        public float TEXCOORD5_X;
        public float TEXCOORD5_Y;
        public float TEXCOORD5_Z;
        public float TEXCOORD5_W;

        public float TEXCOORD6_X;
        public float TEXCOORD6_Y;
        public float TEXCOORD6_Z;
        public float TEXCOORD6_W;

        public float TEXCOORD7_X;
        public float TEXCOORD7_Y;
        public float TEXCOORD7_Z;
        public float TEXCOORD7_W;

        public float COLOR0_X;
        public float COLOR0_Y;
        public float COLOR0_Z;
        public float COLOR0_W;

        public Vector3 POSITION
        {
            get
            {
                return new Vector3(
                POSITION_X,
                POSITION_Y,
                POSITION_Z);
            }
        }

        // jave.lin : 齐次坐标
        public Vector4 POSITION_H
        {
            get
            {
                return new Vector4(
                POSITION_X,
                POSITION_Y,
                POSITION_Z,
                1);
            }
        }

        public Vector4 NORMAL
        {
            get
            {
                return new Vector4(
                NORMAL_X,
                NORMAL_Y,
                NORMAL_Z,
                NORMAL_W);
            }
        }
        public Vector4 TANGENT
        {
            get
            {
                return new Vector4(
                TANGENT_X,
                TANGENT_Y,
                TANGENT_Z,
                TANGENT_W);
            }
        }

        public Vector4 TEXCOORD0
        {
            get
            {
                return new Vector4(
                TEXCOORD0_X,
                TEXCOORD0_Y,
                TEXCOORD0_Z,
                TEXCOORD0_W);
            }
        }

        public Vector4 TEXCOORD1
        {
            get
            {
                return new Vector4(
                TEXCOORD1_X,
                TEXCOORD1_Y,
                TEXCOORD1_Z,
                TEXCOORD1_W);
            }
        }

        public Vector4 TEXCOORD2
        {
            get
            {
                return new Vector4(
                TEXCOORD2_X,
                TEXCOORD2_Y,
                TEXCOORD2_Z,
                TEXCOORD2_W);
            }
        }

        public Vector4 TEXCOORD3
        {
            get
            {
                return new Vector4(
                TEXCOORD3_X,
                TEXCOORD3_Y,
                TEXCOORD3_Z,
                TEXCOORD3_W);
            }
        }

        public Vector4 TEXCOORD4
        {
            get
            {
                return new Vector4(
                TEXCOORD4_X,
                TEXCOORD4_Y,
                TEXCOORD4_Z,
                TEXCOORD4_W);
            }
        }

        public Vector4 TEXCOORD5
        {
            get
            {
                return new Vector4(
                TEXCOORD5_X,
                TEXCOORD5_Y,
                TEXCOORD5_Z,
                TEXCOORD5_W);
            }
        }

        public Vector4 TEXCOORD6
        {
            get
            {
                return new Vector4(
                TEXCOORD6_X,
                TEXCOORD6_Y,
                TEXCOORD6_Z,
                TEXCOORD6_W);
            }
        }

        public Vector4 TEXCOORD7
        {
            get
            {
                return new Vector4(
                TEXCOORD7_X,
                TEXCOORD7_Y,
                TEXCOORD7_Z,
                TEXCOORD7_W);
            }
        }

        public Color COLOR0
        {
            get
            {
                return new Color(
                COLOR0_X,
                COLOR0_Y,
                COLOR0_Z,
                COLOR0_W);
            }
        }
    }

    private const string GO_Parent_Name = "Models_From_CSV";

    // jave.lin : on_gui 上显示的对象
    private UnityEngine.Object role_folder_obj;
    private TextAsset position_CSV_TextAsset;
    private TextAsset index_CSV_TextAsset;
    private TextAsset tangent_CSV_TextAsset;
    private TextAsset normal_CSV_TextAsset;
    private TextAsset uv0_CSV_TextAsset;
    private TextAsset uv1_CSV_TextAsset;
    private TextAsset uv2_CSV_TextAsset;
    private TextAsset uv3_CSV_TextAsset;
    private TextAsset uv4_CSV_TextAsset;
    private TextAsset uv5_CSV_TextAsset;
    private TextAsset uv6_CSV_TextAsset;
    private TextAsset uv7_CSV_TextAsset;
    private TextAsset color0_CSV_TextAsset;
    private VBVChannelNormalizedType color0NormlizedType;

    private bool include_tangent;
    private bool include_normal;
    private bool include_uv0;
    private bool include_uv1;
    private bool include_uv2;
    private bool include_uv3;
    private bool include_uv4;
    private bool include_uv5;
    private bool include_uv6;
    private bool include_uv7;
    private bool flip_uv0_y = true;
    private bool flip_uv1_y = false;
    private bool flip_uv2_y = false;
    private bool flip_uv3_y = false;
    private bool flip_uv4_y = false;
    private bool flip_uv5_y = false;
    private bool flip_uv6_y = false;
    private bool flip_uv7_y = false;
    private bool include_color0;

    private string modelName;
    private string outputDir;
    private string outputModelPrefabFullName;
    private string outputUnityMeshFullName;

    // jave.lin : on_gui - options
    private Vector2 optionsScrollPos;
    private bool options_show = true;
    private bool is_from_DX_CSV = true;
    private Vector3 vertexOffset = Vector3.zero;
    private Vector3 vertexRotation = Vector3.zero;
    private Vector3 vertexScale = Vector3.one;
    private bool is_reverse_vertex_order = false; // jave.lin : for reverse normal
    private bool is_recalculate_bound = true;
    private ModelImporterNormals normalImportType = ModelImporterNormals.Import;
    private ModelImporterTangents tangentImportType = ModelImporterTangents.Import;
    private bool show_mat_toggle = true;
    private MaterialSetType materialSetType = MaterialSetType.CreateNew;
    private Shader shader;
    private Texture albedo_texture;
    private Texture normal_texture;
    private Material material;

    private ExportFileType exportFileType;

    private bool model_readable = false;

    private Dictionary<string, SemanticType> semanticTypeDict_key_name_helper;

    // jave.lin : 删除指定目录+目录下的所有文件
    private void DelectDir(string dir)
    {
        try
        {
            if (!Directory.Exists(outputDir))
                return;

            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            // 返回目录中所有文件和子目录
            FileSystemInfo[] fileInfos = dirInfo.GetFileSystemInfos();
            foreach (FileSystemInfo fileInfo in fileInfos)
            {
                if (fileInfo is DirectoryInfo)
                {
                    // 判断是否文件夹
                    DirectoryInfo subDir = new DirectoryInfo(fileInfo.FullName);
                    subDir.Delete(true);            // 删除子目录和文件
                }
                else
                {
                    File.Delete(fileInfo.FullName);      // 删除指定文件
                }
            }
        }
        catch (Exception e)
        {
            throw e;
        }
    }


    public void ResetData()
    {
        role_folder_obj = null;
        position_CSV_TextAsset = null;
        index_CSV_TextAsset = null;
        tangent_CSV_TextAsset = null;
        normal_CSV_TextAsset = null;
        uv0_CSV_TextAsset = null;
        uv1_CSV_TextAsset = null;
        uv2_CSV_TextAsset = null;
        uv3_CSV_TextAsset = null;
        uv4_CSV_TextAsset = null;
        uv5_CSV_TextAsset = null;
        uv6_CSV_TextAsset = null;
        uv7_CSV_TextAsset = null;
        color0_CSV_TextAsset = null;

        color0NormlizedType = default;

        include_tangent = default;
        include_normal = default;
        include_uv0 = default;
        include_uv1 = default;
        include_uv2 = default;
        include_uv3 = default;
        include_uv4 = default;
        include_uv5 = default;
        include_uv6 = default;
        include_uv7 = default;
        flip_uv0_y = true;
        flip_uv1_y = default;
        flip_uv2_y = default;
        flip_uv3_y = default;
        flip_uv4_y = default;
        flip_uv5_y = default;
        flip_uv6_y = default;
        flip_uv7_y = default;
        include_color0 = default;

        modelName = default;
        outputDir = default;
        outputModelPrefabFullName = default;

        optionsScrollPos = default;
        options_show = true;
        is_from_DX_CSV = true;
        vertexOffset = Vector3.zero;
        vertexRotation = Vector3.zero;
        vertexScale = Vector3.one;
        is_reverse_vertex_order = false;
        is_recalculate_bound = true;
        normalImportType = ModelImporterNormals.Import;
        tangentImportType = ModelImporterTangents.Import;
        show_mat_toggle = true;
        materialSetType = MaterialSetType.CreateNew;
        shader = default;
        albedo_texture = default;
        normal_texture = default;
        material = default;
        semanticTypeDict_key_name_helper = default;
    }

    // jave.lin : 根据全路径名 转换为 assets 目录下的名字
    private string GetAssetPathByFullName(string fullName)
    {
        fullName = fullName.Replace("\\", "/");
        var dataPath_prefix = Application.dataPath.Replace("Assets", "");
        dataPath_prefix = dataPath_prefix.Replace(dataPath_prefix + "/", "");
        var mi_path = fullName.Replace(dataPath_prefix, "");
        return mi_path;
    }

    private void OnGUI()
    {
        Output_RDC_CSV_Handle_GUI();
    }

    // jave.lin : 获取Shader
    private Shader GetShader(string custom_shader_name = null)
    {
        Shader ret = null;
        if (string.IsNullOrEmpty(custom_shader_name))
            ret = Shader.Find(custom_shader_name); // jave.lin : custom special
        if (ret == null) // jave.lin : BRP standard
            ret = Shader.Find("Standard");
        if (ret == null)
            ret = Shader.Find("Universal Render Pipeline/Lit"); // jave.lin : URP Lit
        if (ret == null)
            Debug.LogError($"找不到 BRP Standard shader 或是 URP Lit shader");

        return ret;
    }

    private void Output_RDC_CSV_Handle_GUI()
    {
        // jave.lin : 角色文件夹
        var role_folder_refresh = false;
        var role_folder = string.Empty;

        UnityEngine.Object new_role_folder_obj = null;

        EditorGUILayout.BeginHorizontal();

        new_role_folder_obj = EditorGUILayout.ObjectField("role_folder", role_folder_obj, typeof(UnityEngine.Object), false);
        if (GUILayout.Button("Refresh"))
        {
            role_folder_obj = null;
        }

        EditorGUILayout.EndHorizontal();

        if (role_folder_obj != new_role_folder_obj)
        {
            ResetData();
            role_folder_obj = new_role_folder_obj;
            role_folder_refresh = true;

            if (role_folder_obj != null)
            {
                role_folder = AssetDatabase.GetAssetPath(role_folder_obj);

                var pos_csv = AssetDatabase.LoadAssetAtPath($"{role_folder}/position.csv", typeof(TextAsset)) as TextAsset;
                var index_csv = AssetDatabase.LoadAssetAtPath($"{role_folder}/index.csv", typeof(TextAsset)) as TextAsset;
                var tangent_csv = AssetDatabase.LoadAssetAtPath($"{role_folder}/tangent.csv", typeof(TextAsset)) as TextAsset;
                var normal_csv = AssetDatabase.LoadAssetAtPath($"{role_folder}/normal.csv", typeof(TextAsset)) as TextAsset;
                var uv0_csv = AssetDatabase.LoadAssetAtPath($"{role_folder}/uv0.csv", typeof(TextAsset)) as TextAsset;
                var uv1_csv = AssetDatabase.LoadAssetAtPath($"{role_folder}/uv1.csv", typeof(TextAsset)) as TextAsset;
                var uv2_csv = AssetDatabase.LoadAssetAtPath($"{role_folder}/uv2.csv", typeof(TextAsset)) as TextAsset;
                var uv3_csv = AssetDatabase.LoadAssetAtPath($"{role_folder}/uv3.csv", typeof(TextAsset)) as TextAsset;
                var uv4_csv = AssetDatabase.LoadAssetAtPath($"{role_folder}/uv4.csv", typeof(TextAsset)) as TextAsset;
                var uv5_csv = AssetDatabase.LoadAssetAtPath($"{role_folder}/uv5.csv", typeof(TextAsset)) as TextAsset;
                var uv6_csv = AssetDatabase.LoadAssetAtPath($"{role_folder}/uv6.csv", typeof(TextAsset)) as TextAsset;
                var uv7_csv = AssetDatabase.LoadAssetAtPath($"{role_folder}/uv7.csv", typeof(TextAsset)) as TextAsset;
                var color0_csv = AssetDatabase.LoadAssetAtPath($"{role_folder}/color0.csv", typeof(TextAsset)) as TextAsset;

                var albedo_tex = AssetDatabase.LoadAssetAtPath($"{role_folder}/albedo.png", typeof(Texture)) as Texture;
                var normal_tex = AssetDatabase.LoadAssetAtPath($"{role_folder}/normal.png", typeof(Texture)) as Texture;

                if (albedo_tex != null) albedo_texture = albedo_tex;
                if (normal_tex != null) normal_texture = normal_tex;

                if (pos_csv != null) position_CSV_TextAsset = pos_csv;
                if (index_csv != null) index_CSV_TextAsset = index_csv;
                if (tangent_csv != null)
                {
                    include_tangent = true;
                    tangent_CSV_TextAsset = tangent_csv;
                }
                if (normal_csv != null)
                {
                    include_normal = true;
                    normal_CSV_TextAsset = normal_csv;
                }
                if (uv0_csv != null)
                {
                    include_uv0 = true;
                    uv0_CSV_TextAsset = uv0_csv;
                }
                if (uv1_csv != null)
                {
                    include_uv1 = true;
                    uv1_CSV_TextAsset = uv1_csv;
                }
                if (uv2_csv != null)
                {
                    include_uv2 = true;
                    uv2_CSV_TextAsset = uv2_csv;
                }
                if (uv3_csv != null)
                {
                    include_uv3 = true;
                    uv3_CSV_TextAsset = uv3_csv;
                }
                if (uv4_csv != null)
                {
                    include_uv4 = true;
                    uv4_CSV_TextAsset = uv4_csv;
                }
                if (uv5_csv != null)
                {
                    include_uv5 = true;
                    uv5_CSV_TextAsset = uv5_csv;
                }
                if (uv6_csv != null)
                {
                    include_uv6 = true;
                    uv6_CSV_TextAsset = uv6_csv;
                }
                if (uv7_csv != null)
                {
                    include_uv7 = true;
                    uv7_CSV_TextAsset = uv7_csv;
                }
                if (color0_csv != null)
                {
                    include_color0 = true;
                    color0_CSV_TextAsset = color0_csv;
                }

                Debug.Log($"Role Folder Name : {role_folder_obj.name}");
            }
        }

        // jave.lin : position 是必选项，不用 toggle
        var new_position_CSV_TextAsset = EditorGUILayout.ObjectField("position_CSV", position_CSV_TextAsset, typeof(TextAsset), false) as TextAsset;
        var new_index_CSV_TextAsset = EditorGUILayout.ObjectField("index_CSV", index_CSV_TextAsset, typeof(TextAsset), false) as TextAsset;

        const int WIDTH_1 = 180;
        const int WIDTH_2 = 180;
        const int GAP = 3;
        EditorGUILayout.BeginHorizontal();
        include_tangent = EditorGUILayout.Toggle("include tangent", include_tangent, GUILayout.Width(WIDTH_1 + WIDTH_2 + GAP));
        if (include_tangent) tangent_CSV_TextAsset      = EditorGUILayout.ObjectField("tangent_CSV", tangent_CSV_TextAsset, typeof(TextAsset), false) as TextAsset;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        include_normal = EditorGUILayout.Toggle("include normal", include_normal, GUILayout.Width(WIDTH_1 + WIDTH_2 + GAP));
        if (include_normal) normal_CSV_TextAsset        = EditorGUILayout.ObjectField("normal_CSV", normal_CSV_TextAsset, typeof(TextAsset), false) as TextAsset;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        include_uv0 = EditorGUILayout.Toggle("include uv0", include_uv0, GUILayout.Width(WIDTH_1));
        if (include_uv0)
        {
            flip_uv0_y = EditorGUILayout.Toggle("flip y(if data suggest false)", flip_uv0_y, GUILayout.Width(WIDTH_2));
            uv0_CSV_TextAsset = EditorGUILayout.ObjectField("uv0_CSV", uv0_CSV_TextAsset, typeof(TextAsset), false) as TextAsset;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        include_uv1 = EditorGUILayout.Toggle("include uv1", include_uv1, GUILayout.Width(WIDTH_1));
        if (include_uv1)
        {
            flip_uv1_y = EditorGUILayout.Toggle("flip y(if data suggest false)", flip_uv1_y, GUILayout.Width(WIDTH_2));
            if (include_uv1) uv1_CSV_TextAsset = EditorGUILayout.ObjectField("uv1_CSV", uv1_CSV_TextAsset, typeof(TextAsset), false) as TextAsset;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        include_uv2 = EditorGUILayout.Toggle("include uv2", include_uv2, GUILayout.Width(WIDTH_1));
        if (include_uv2)
        {
            flip_uv2_y = EditorGUILayout.Toggle("flip y(if data suggest false)", flip_uv2_y, GUILayout.Width(WIDTH_2));
            uv2_CSV_TextAsset = EditorGUILayout.ObjectField("uv2_CSV", uv2_CSV_TextAsset, typeof(TextAsset), false) as TextAsset;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        include_uv3 = EditorGUILayout.Toggle("include uv3", include_uv3, GUILayout.Width(WIDTH_1));
        if (include_uv3)
        {
            flip_uv3_y = EditorGUILayout.Toggle("flip y(if data suggest false)", flip_uv3_y, GUILayout.Width(WIDTH_2));
            uv3_CSV_TextAsset = EditorGUILayout.ObjectField("uv3_CSV", uv3_CSV_TextAsset, typeof(TextAsset), false) as TextAsset;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        include_uv4 = EditorGUILayout.Toggle("include uv4", include_uv4, GUILayout.Width(WIDTH_1));
        if (include_uv4)
        {
            flip_uv4_y = EditorGUILayout.Toggle("flip y(if data suggest false)", flip_uv4_y, GUILayout.Width(WIDTH_2));
            uv4_CSV_TextAsset = EditorGUILayout.ObjectField("uv4_CSV", uv4_CSV_TextAsset, typeof(TextAsset), false) as TextAsset;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        include_uv5 = EditorGUILayout.Toggle("include uv5", include_uv5, GUILayout.Width(WIDTH_1));
        if (include_uv5)
        {
            flip_uv5_y = EditorGUILayout.Toggle("flip y(if data suggest false)", flip_uv5_y, GUILayout.Width(WIDTH_2));
            uv5_CSV_TextAsset = EditorGUILayout.ObjectField("uv5_CSV", uv5_CSV_TextAsset, typeof(TextAsset), false) as TextAsset;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        include_uv6 = EditorGUILayout.Toggle("include uv6", include_uv6, GUILayout.Width(WIDTH_1));
        if (include_uv6)
        {
            flip_uv6_y = EditorGUILayout.Toggle("flip y(if data suggest false)", flip_uv6_y, GUILayout.Width(WIDTH_2));
            uv6_CSV_TextAsset = EditorGUILayout.ObjectField("uv6_CSV", uv6_CSV_TextAsset, typeof(TextAsset), false) as TextAsset;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        include_uv7 = EditorGUILayout.Toggle("include uv7", include_uv7, GUILayout.Width(WIDTH_1));
        if (include_uv7)
        {
            flip_uv7_y = EditorGUILayout.Toggle("flip y(if data suggest false)", flip_uv7_y, GUILayout.Width(WIDTH_2));
            uv7_CSV_TextAsset = EditorGUILayout.ObjectField("uv7_CSV", uv7_CSV_TextAsset, typeof(TextAsset), false) as TextAsset;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        include_color0 = EditorGUILayout.Toggle("include color0", include_color0);
        if (include_color0)
        {
            color0_CSV_TextAsset = EditorGUILayout.ObjectField("color0_CSV", color0_CSV_TextAsset, typeof(TextAsset), false) as TextAsset;
            color0NormlizedType = (VBVChannelNormalizedType)EditorGUILayout.EnumPopup("VBV Channel Normalized Type", color0NormlizedType);
        }
        EditorGUILayout.EndHorizontal();

        // jave.lin : 如果 CSV 切换了
        if (role_folder_refresh || position_CSV_TextAsset != new_position_CSV_TextAsset)
        {
            position_CSV_TextAsset = new_position_CSV_TextAsset;
        }
        if (role_folder_refresh || index_CSV_TextAsset != new_index_CSV_TextAsset)
        {
            index_CSV_TextAsset = new_index_CSV_TextAsset;
        }

        if (position_CSV_TextAsset == null)
        {
            var srcCol = GUI.contentColor;
            GUI.contentColor = Color.red;
            EditorGUILayout.LabelField("Have no setting the position_CSV yet!");
            GUI.contentColor = srcCol;
            return;
        }

        if (index_CSV_TextAsset == null)
        {
            var srcCol = GUI.contentColor;
            GUI.contentColor = Color.red;
            EditorGUILayout.LabelField("Have no setting the indices_CSV yet!");
            GUI.contentColor = srcCol;
            return;
        }

        if (role_folder_refresh)
        {
            material = null;
        }

        // jave.lin : 模型名字
        modelName = EditorGUILayout.TextField("Model Name", modelName);
        if (position_CSV_TextAsset != null && (role_folder_refresh || string.IsNullOrEmpty(modelName)))
        {
            modelName = role_folder_obj.name;
        }

        // jave.lin : output path
        EditorGUILayout.BeginHorizontal();
        outputDir = EditorGUILayout.TextField("Output Path(Dir)", outputDir);
        if (role_folder_refresh || string.IsNullOrEmpty(outputDir))
        {
            // jave.lin : 拼接生成路径
            outputDir = Path.Combine(Application.dataPath, $"Models_From_CSV/{modelName}");
            outputDir = outputDir.Replace("\\", "/");
        }
        if (GUILayout.Button("Browser...", GUILayout.Width(100)))
        {
            outputDir = EditorUtility.OpenFolderPanel("Select an output path", outputDir, "");
        }
        if (GUILayout.Button("Pin", GUILayout.Width(100)))
        {
            var folderPath = "";
            if (outputDir.Contains(Application.dataPath))
            {
                folderPath = "Assets" + outputDir.Replace(Application.dataPath, "");
            }
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
            if (obj != null) EditorGUIUtility.PingObject(obj);
        }
        EditorGUILayout.EndHorizontal();
        // jave.lin : 显示导出的 full name
        GUI.enabled = false;
        if (exportFileType == ExportFileType.FBX)
        {
            outputModelPrefabFullName = Path.Combine(outputDir, modelName + ".fbx");
            outputModelPrefabFullName = outputModelPrefabFullName.Replace("\\", "/");
            EditorGUILayout.TextField("Output Full Name", outputModelPrefabFullName);
        }
        else
        {
            outputUnityMeshFullName = Path.Combine(outputDir, modelName + "_mesh.asset");
            outputUnityMeshFullName = outputUnityMeshFullName.Replace("\\", "/");
            outputModelPrefabFullName = Path.Combine(outputDir, modelName + ".prefab");
            outputModelPrefabFullName = outputModelPrefabFullName.Replace("\\", "/");
            EditorGUILayout.TextField("Output Unity Mesh Full Name", outputUnityMeshFullName);
            EditorGUILayout.TextField("Output Model Prefab Full Name", outputModelPrefabFullName);
        }
        GUI.enabled = true;

        // 显示注意选项
        {
            var src_color = GUI.contentColor;
            GUI.contentColor = Color.red;
            GUILayout.Label("！！！注意注意！！！如果vertex input 里面的 TEXCOORD[n] 有使用到超过2个分量的数据类型，那么需要使用 UnityMesh 的导出类型，否则FBX中无法存储");

            GUI.contentColor = Color.green;
            if (GUILayout.Button("点击我，了解苦逼测试历程"))
            {
                Application.OpenURL("https://blog.csdn.net/linjf520/article/details/133993603");
            }

            GUI.contentColor = Color.yellow;
            exportFileType = (ExportFileType)EditorGUILayout.EnumPopup("导出文件类型", exportFileType);
            GUI.contentColor = src_color;
        }

        if (exportFileType == ExportFileType.FBX)
        {
            // jave.lin : 导出 CSV 对应的 FBX
            if (GUILayout.Button("Export FBX"))
            {
                ExportFBXHandle();
            }
        }
        else
        {
            // jave.lin : 导出 CSV 对应的 UnityMesh
            if (GUILayout.Button("Export UnityMesh"))
            {
                ExportUnityMeshHandle();
            }
        }

        // jave.lin : 显示 scroll view
        optionsScrollPos = EditorGUILayout.BeginScrollView(optionsScrollPos);

        // jave.lin : options 内容
        EditorGUILayout.Space(10);
        options_show = EditorGUILayout.BeginFoldoutHeaderGroup(options_show, "Model Options");
        if (options_show)
        {
            EditorGUI.indentLevel++;
            model_readable = EditorGUILayout.Toggle("Model Readable", model_readable);
            // jave.lin : 是否从 dx 的 Graphics API 导出而来的 CSV
            is_from_DX_CSV = EditorGUILayout.Toggle("Is From DirectX CSV", is_from_DX_CSV);
            // jave.lin : 是否反转法线 : 通过反转 indices 的顺序即可达到效果
            is_reverse_vertex_order = EditorGUILayout.Toggle("Is Reverse Normal", is_reverse_vertex_order);
            // jave.lin : 是否重新计算 AABB
            is_recalculate_bound = EditorGUILayout.Toggle("Is Recalculate AABB", is_recalculate_bound);
            // jave.lin : 顶点平移
            vertexOffset = EditorGUILayout.Vector3Field("Vertex Offset", vertexOffset);
            // jave.lin : 顶点旋转
            vertexRotation = EditorGUILayout.Vector3Field("Vertex Rotation", vertexRotation);
            // jave.lin : 顶点缩放
            vertexScale = EditorGUILayout.Vector3Field("Vertex Scale", vertexScale);
            // jave.lin : 法线导入方式
            normalImportType = (ModelImporterNormals)EditorGUILayout.EnumPopup("Normal Import Type", normalImportType);
            // jave.lin : 切线导入方式
            tangentImportType = (ModelImporterTangents)EditorGUILayout.EnumPopup("Tangent Import Type", tangentImportType);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // jave.lin : 是否自动创建材质
        EditorGUILayout.Space(10);
        show_mat_toggle = EditorGUILayout.BeginFoldoutHeaderGroup(show_mat_toggle, "Material Options");
        if (show_mat_toggle)
        {
            EditorGUI.indentLevel++;
            var newMaterialSetType = (MaterialSetType)EditorGUILayout.EnumPopup("Material Set Type", materialSetType);
            if (material == null || materialSetType != newMaterialSetType)
            {
                materialSetType = newMaterialSetType;
                // jave.lin : 创建
                if (materialSetType == MaterialSetType.CreateNew)
                {
                    // jave.lin : shader 不能为空
                    if (shader == null)
                    {
                        shader = GetShader();
                    }
                    material = new Material(shader);
                }
                else
                {
                    // jave.lin : 默认使用 导出目录下的 mat 材质
                    var mat_path = Path.Combine(outputDir, modelName + ".mat").Replace("\\", "/");
                    mat_path = GetAssetPathByFullName(mat_path);
                    var mat_asset = AssetDatabase.LoadAssetAtPath<Material>(mat_path);
                    if (mat_asset != null) material = mat_asset;
                }
            }

            if (materialSetType == MaterialSetType.CreateNew)
            {
                // jave.lin : 使用的 shader
                shader = EditorGUILayout.ObjectField("Shader", shader, typeof(Shader), false) as Shader;
                // jave.lin : 使用的 主纹理
                albedo_texture = EditorGUILayout.ObjectField("Main Texture", albedo_texture, typeof(Texture), false) as Texture;
                // jave.lin : 使用的 法线
                normal_texture = EditorGUILayout.ObjectField("Normal Texture", normal_texture, typeof(Texture), false) as Texture;
            }
            // jave.lin : 设置
            else // MaterialSetType.UseExsitMaterialAsset
            {
                material = EditorGUILayout.ObjectField("Material Asset", material, typeof(Material), false) as Material;
            }

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.EndScrollView();
    }

    // jave.lin : 导出FBX处理
    private void ExportFBXHandle()
    {
        if (position_CSV_TextAsset != null)
        {
            try
            {
                // jave.lin : 清理之前的 GO
                var parent = GetParentTrans();
                // jave.lin : 将 CSV 的内容转为 MeshRenderer 的 GO
                var outputGO = GameObject.Find($"{GO_Parent_Name}/{modelName}");
                if (outputGO != null)
                {
                    GameObject.DestroyImmediate(outputGO);
                }
                
                var vbv_csv_List = new List<string>();
                vbv_csv_List.Add(position_CSV_TextAsset.text);

                if (include_tangent && tangent_CSV_TextAsset != null) vbv_csv_List.Add(tangent_CSV_TextAsset.text);
                if (include_normal && normal_CSV_TextAsset != null) vbv_csv_List.Add(normal_CSV_TextAsset.text);
                if (include_uv0 && uv0_CSV_TextAsset != null) vbv_csv_List.Add(uv0_CSV_TextAsset.text);
                if (include_uv0 && uv1_CSV_TextAsset != null) vbv_csv_List.Add(uv1_CSV_TextAsset.text);
                if (include_uv0 && uv2_CSV_TextAsset != null) vbv_csv_List.Add(uv2_CSV_TextAsset.text);
                if (include_uv0 && uv3_CSV_TextAsset != null) vbv_csv_List.Add(uv3_CSV_TextAsset.text);
                if (include_uv0 && uv4_CSV_TextAsset != null) vbv_csv_List.Add(uv4_CSV_TextAsset.text);
                if (include_uv0 && uv5_CSV_TextAsset != null) vbv_csv_List.Add(uv5_CSV_TextAsset.text);
                if (include_uv0 && uv6_CSV_TextAsset != null) vbv_csv_List.Add(uv6_CSV_TextAsset.text);
                if (include_uv0 && uv7_CSV_TextAsset != null) vbv_csv_List.Add(uv7_CSV_TextAsset.text);
                if (include_color0 && color0_CSV_TextAsset != null) vbv_csv_List.Add(color0_CSV_TextAsset.text);

                // jave.lin : 生成 uv y flip 标记字典
                var flip_UV_YCoordType_Dict = new Dictionary<Flip_UV_YCoordsType, bool>();
                if (flip_uv0_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD0_Y] = true;
                if (flip_uv1_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD1_Y] = true;
                if (flip_uv2_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD2_Y] = true;
                if (flip_uv3_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD3_Y] = true;
                if (flip_uv4_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD4_Y] = true;
                if (flip_uv5_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD5_Y] = true;
                if (flip_uv6_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD6_Y] = true;
                if (flip_uv7_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD7_Y] = true;

                // jave.lin : 根据 CSV 生成 带 MeshRenderer 的 GO
                outputGO = GenerateGOWithMeshRendererFromCSV(vbv_csv_List, index_CSV_TextAsset.text, is_from_DX_CSV, flip_UV_YCoordType_Dict);
                outputGO.transform.SetParent(parent);
                outputGO.name = modelName;

                //// jave.lin : 先清理目录下的内容
                //DelectDir(outputDir);
                // jave.lin : 然后重新创建新的目录
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                if (materialSetType == MaterialSetType.CreateNew)
                {
                    var mat_created_path = Path.Combine(outputDir, modelName + ".mat").Replace("\\", "/");
                    mat_created_path = GetAssetPathByFullName(mat_created_path);

                    var src_applied_mat = AssetDatabase.LoadAssetAtPath<Material>(mat_created_path);

                    var src_mat = outputGO.GetComponent<MeshRenderer>().sharedMaterial;
                    if (src_applied_mat != null && src_mat == src_applied_mat)
                    {
                        // jave.lin : 和原来一样的 材质实例，则什么都不用处理
                    }
                    else
                    {
                        // jave.lin : 自动创建材质
                        var custom_shader = GetShader("PBR_MRA_SSS");
                        var create_mat = new Material(custom_shader);
                        // jave.lin : 创建前，先设置主纹理
                        create_mat.SetTexture("_MainTex", albedo_texture);
                        create_mat.SetTexture("_BumpMap", normal_texture);
                        // jave.lin : 应用材质
                        outputGO.GetComponent<MeshRenderer>().sharedMaterial = create_mat;

                        Debug.Log($"mat_created_path : {mat_created_path}");

                        // jave.lin : 先删除原来的
                        AssetDatabase.DeleteAsset(mat_created_path);

                        AssetDatabase.CreateAsset(create_mat, mat_created_path);
                    }
                }
                else
                {
                    outputGO.GetComponent<MeshRenderer>().sharedMaterial = material;
                }

                // jave.lin : 使用 FBX Exporter 插件导出 FBX
                ModelExporter.ExportObject(outputModelPrefabFullName, outputGO);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // jave.lin : 重新设置 MI，并且重新导入
                string mi_path = GetAssetPathByFullName(outputModelPrefabFullName);
                ModelImporter mi = ModelImporter.GetAtPath(mi_path) as ModelImporter;
                mi.importNormals = normalImportType;
                mi.importTangents = tangentImportType;
                mi.importAnimation = false;
                mi.importAnimatedCustomProperties = false;
                mi.importBlendShapeNormals = ModelImporterNormals.None;
                mi.importBlendShapes = false;
                mi.importCameras = false;
                mi.importConstraints = false;
                mi.importLights = false;
                mi.importVisibility = false;
                mi.animationType = ModelImporterAnimationType.None;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.isReadable = model_readable;
                mi.SaveAndReimport();

                // jave.lin : replace outputGO from model prefab
                var src_parent = outputGO.transform.parent;
                var src_local_pos = outputGO.transform.localPosition;
                var src_local_rot = outputGO.transform.localRotation;
                var src_local_scl = outputGO.transform.localScale;
                DestroyImmediate(outputGO);
                // jave.lin : new model prefab
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(mi_path);
                outputGO = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                outputGO.transform.SetParent(src_parent);
                outputGO.transform.localPosition = src_local_pos;
                outputGO.transform.localRotation = src_local_rot;
                outputGO.transform.localScale = src_local_scl;
                outputGO.name = modelName;
                // jave.lin : set material
                if (materialSetType == MaterialSetType.CreateNew)
                {
                    var mat_path = Path.Combine(outputDir, modelName + ".mat").Replace("\\", "/");
                    mat_path = GetAssetPathByFullName(mat_path);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(mat_path);
                    outputGO.GetComponent<MeshRenderer>().sharedMaterial = mat;
                }
                else
                {
                    outputGO.GetComponent<MeshRenderer>().sharedMaterial = material;
                }
                // jave.lin : new real regular prefab
                var prefab_created_path = Path.Combine(outputDir, modelName + ".prefab").Replace("\\", "/");
                prefab_created_path = GetAssetPathByFullName(prefab_created_path);
                Debug.Log($"prefab_created_path : {prefab_created_path}");
                PrefabUtility.SaveAsPrefabAssetAndConnect(outputGO, prefab_created_path, InteractionMode.AutomatedAction);

                // jave.lin : 打印打出成功的信息
                Debug.Log($"Export FBX Successfully! outputPath : {outputModelPrefabFullName}");
            }
            catch (Exception er)
            {
                Debug.LogError($"Export FBX Failed! er: {er}");
            }
        }
    }

    // jave.lin : 导出UnityMesh处理
    private void ExportUnityMeshHandle()
    {
        if (position_CSV_TextAsset != null)
        {
            try
            {
                // jave.lin : 清理之前的 GO
                var parent = GetParentTrans();
                // jave.lin : 将 CSV 的内容转为 MeshRenderer 的 GO
                var outputGO = GameObject.Find($"{GO_Parent_Name}/{modelName}");
                if (outputGO != null)
                {
                    GameObject.DestroyImmediate(outputGO);
                }

                var vbv_csv_List = new List<string>();
                vbv_csv_List.Add(position_CSV_TextAsset.text);

                if (include_tangent && tangent_CSV_TextAsset != null) vbv_csv_List.Add(tangent_CSV_TextAsset.text);
                if (include_normal && normal_CSV_TextAsset != null) vbv_csv_List.Add(normal_CSV_TextAsset.text);
                if (include_uv0 && uv0_CSV_TextAsset != null) vbv_csv_List.Add(uv0_CSV_TextAsset.text);
                if (include_uv0 && uv1_CSV_TextAsset != null) vbv_csv_List.Add(uv1_CSV_TextAsset.text);
                if (include_uv0 && uv2_CSV_TextAsset != null) vbv_csv_List.Add(uv2_CSV_TextAsset.text);
                if (include_uv0 && uv3_CSV_TextAsset != null) vbv_csv_List.Add(uv3_CSV_TextAsset.text);
                if (include_uv0 && uv4_CSV_TextAsset != null) vbv_csv_List.Add(uv4_CSV_TextAsset.text);
                if (include_uv0 && uv5_CSV_TextAsset != null) vbv_csv_List.Add(uv5_CSV_TextAsset.text);
                if (include_uv0 && uv6_CSV_TextAsset != null) vbv_csv_List.Add(uv6_CSV_TextAsset.text);
                if (include_uv0 && uv7_CSV_TextAsset != null) vbv_csv_List.Add(uv7_CSV_TextAsset.text);
                if (include_color0 && color0_CSV_TextAsset != null) vbv_csv_List.Add(color0_CSV_TextAsset.text);

                // jave.lin : 生成 uv y flip 标记字典
                var flip_UV_YCoordType_Dict = new Dictionary<Flip_UV_YCoordsType, bool>();
                if (flip_uv0_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD0_Y] = true;
                if (flip_uv1_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD1_Y] = true;
                if (flip_uv2_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD2_Y] = true;
                if (flip_uv3_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD3_Y] = true;
                if (flip_uv4_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD4_Y] = true;
                if (flip_uv5_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD5_Y] = true;
                if (flip_uv6_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD6_Y] = true;
                if (flip_uv7_y) flip_UV_YCoordType_Dict[Flip_UV_YCoordsType.TEXCOORD7_Y] = true;

                // jave.lin : 根据 CSV 生成 带 MeshRenderer 的 GO
                outputGO = GenerateGOWithMeshRendererFromCSV(vbv_csv_List, index_CSV_TextAsset.text, is_from_DX_CSV, flip_UV_YCoordType_Dict);
                outputGO.transform.SetParent(parent);
                outputGO.name = modelName;

                //// jave.lin : 先清理目录下的内容
                //DelectDir(outputDir);
                // jave.lin : 然后重新创建新的目录
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                if (materialSetType == MaterialSetType.CreateNew)
                {
                    var mat_created_path = Path.Combine(outputDir, modelName + ".mat").Replace("\\", "/");
                    mat_created_path = GetAssetPathByFullName(mat_created_path);

                    var src_applied_mat = AssetDatabase.LoadAssetAtPath<Material>(mat_created_path);

                    var src_mat = outputGO.GetComponent<MeshRenderer>().sharedMaterial;
                    if (src_applied_mat != null && src_mat == src_applied_mat)
                    {
                        // jave.lin : 和原来一样的 材质实例，则什么都不用处理
                    }
                    else
                    {
                        // jave.lin : 自动创建材质
                        var custom_shader = GetShader("PBR_MRA_SSS");
                        var create_mat = new Material(custom_shader);
                        // jave.lin : 创建前，先设置主纹理
                        create_mat.SetTexture("_MainTex", albedo_texture);
                        create_mat.SetTexture("_BumpMap", normal_texture);
                        // jave.lin : 应用材质
                        outputGO.GetComponent<MeshRenderer>().sharedMaterial = create_mat;

                        Debug.Log($"mat_created_path : {mat_created_path}");

                        // jave.lin : 先删除原来的
                        AssetDatabase.DeleteAsset(mat_created_path);

                        AssetDatabase.CreateAsset(create_mat, mat_created_path);
                    }
                }
                else
                {
                    outputGO.GetComponent<MeshRenderer>().sharedMaterial = material;
                }

                // jave.lin : 导出 unity mesh 保留 uv 超过 2 个分量以上的数据
                var mesh = outputGO.GetComponent<MeshFilter>().sharedMesh;
                MeshUtility.Optimize(mesh);
                var so = new SerializedObject(mesh);
                var sp = so.FindProperty("m_IsReadable");
                sp.boolValue = model_readable;
                so.ApplyModifiedPropertiesWithoutUndo();
                var outputMeshPath = GetAssetPathByFullName(outputUnityMeshFullName);
                AssetDatabase.CreateAsset(mesh, outputMeshPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // jave.lin : 重新设置 mesh
                mesh = AssetDatabase.LoadAssetAtPath<Mesh>(outputMeshPath);
                outputGO.GetComponent<MeshFilter>().sharedMesh = mesh;

                // jave.lin : 导出 prefab
                //AssetDatabase.CreateAsset(outputGO, outputModelPrefabFullName);
                var outputPrefabPath = GetAssetPathByFullName(outputModelPrefabFullName);
                PrefabUtility.SaveAsPrefabAssetAndConnect(outputGO, outputPrefabPath, InteractionMode.AutomatedAction);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // jave.lin : replace outputGO from model prefab
                var src_parent = outputGO.transform.parent;
                var src_local_pos = outputGO.transform.localPosition;
                var src_local_rot = outputGO.transform.localRotation;
                var src_local_scl = outputGO.transform.localScale;
                //DestroyImmediate(outputGO);
                // jave.lin : new model prefab
                //var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputModelPrefabFullName);
                //outputGO = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                outputGO.transform.SetParent(src_parent);
                outputGO.transform.localPosition = src_local_pos;
                outputGO.transform.localRotation = src_local_rot;
                outputGO.transform.localScale = src_local_scl;
                //outputGO.name = modelName;
                // jave.lin : set material
                if (materialSetType == MaterialSetType.CreateNew)
                {
                    var mat_path = Path.Combine(outputDir, modelName + ".mat").Replace("\\", "/");
                    mat_path = GetAssetPathByFullName(mat_path);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(mat_path);
                    outputGO.GetComponent<MeshRenderer>().sharedMaterial = mat;
                }
                else
                {
                    outputGO.GetComponent<MeshRenderer>().sharedMaterial = material;
                }
                // jave.lin : new real regular prefab
                var prefab_created_path = Path.Combine(outputDir, modelName + ".prefab").Replace("\\", "/");
                prefab_created_path = GetAssetPathByFullName(prefab_created_path);
                Debug.Log($"prefab_created_path : {prefab_created_path}");
                PrefabUtility.SaveAsPrefabAssetAndConnect(outputGO, prefab_created_path, InteractionMode.AutomatedAction);

                // jave.lin : 打印打出成功的信息
                Debug.Log($"Export Prefab & UnityMesh Successfully! outputPath : {outputModelPrefabFullName}");
            }
            catch (Exception er)
            {
                Debug.LogError($"Export Prefab & UnityMesh Failed! er: {er}");
            }
        }
    }

    // jave.lin : 映射 semantics 的 name 和 type
    private void MappingSemanticsTypeByNames(ref Dictionary<string, SemanticType> container)
    {
        if (container == null)
        {
            container = new Dictionary<string, SemanticType>();
        }
        else
        {
            container.Clear();
        }
        container["VTX"]                = SemanticType.VTX;
        container["IDX"]                = SemanticType.IDX;
        container["SV_POSITION.X"]      = SemanticType.POSITION_X;
        container["SV_POSITION.Y"]      = SemanticType.POSITION_Y;
        container["SV_POSITION.Z"]      = SemanticType.POSITION_Z;
        container["SV_POSITION.W"]      = SemanticType.POSITION_W;
        container["POSITION.X"]         = SemanticType.POSITION_X;
        container["POSITION.Y"]         = SemanticType.POSITION_Y;
        container["POSITION.Z"]         = SemanticType.POSITION_Z;
        container["POSITION.W"]         = SemanticType.POSITION_W;
        container["POSITION0.X"]        = SemanticType.POSITION_X;
        container["POSITION0.Y"]        = SemanticType.POSITION_Y;
        container["POSITION0.Z"]        = SemanticType.POSITION_Z;
        container["POSITION0.W"]        = SemanticType.POSITION_W;
        container["NORMAL.X"]           = SemanticType.NORMAL_X;
        container["NORMAL.Y"]           = SemanticType.NORMAL_Y;
        container["NORMAL.Z"]           = SemanticType.NORMAL_Z;
        container["NORMAL.W"]           = SemanticType.NORMAL_W;
        container["NORMAL0.X"]          = SemanticType.NORMAL_X;
        container["NORMAL0.Y"]          = SemanticType.NORMAL_Y;
        container["NORMAL0.Z"]          = SemanticType.NORMAL_Z;
        container["NORMAL0.W"]          = SemanticType.NORMAL_W;
        container["TANGENT.X"]          = SemanticType.TANGENT_X;
        container["TANGENT.Y"]          = SemanticType.TANGENT_Y;
        container["TANGENT.Z"]          = SemanticType.TANGENT_Z;
        container["TANGENT.W"]          = SemanticType.TANGENT_W;
        container["TANGENT0.X"]         = SemanticType.TANGENT_X;
        container["TANGENT0.Y"]         = SemanticType.TANGENT_Y;
        container["TANGENT0.Z"]         = SemanticType.TANGENT_Z;
        container["TANGENT0.W"]         = SemanticType.TANGENT_W;
        container["TEXCOORD.X"]         = SemanticType.TEXCOORD0_X;
        container["TEXCOORD.Y"]         = SemanticType.TEXCOORD0_Y;
        container["TEXCOORD.Z"]         = SemanticType.TEXCOORD0_Z;
        container["TEXCOORD.W"]         = SemanticType.TEXCOORD0_W;
        container["TEXCOORD0.X"]        = SemanticType.TEXCOORD0_X;
        container["TEXCOORD0.Y"]        = SemanticType.TEXCOORD0_Y;
        container["TEXCOORD0.Z"]        = SemanticType.TEXCOORD0_Z;
        container["TEXCOORD0.W"]        = SemanticType.TEXCOORD0_W;
        container["TEXCOORD1.X"]        = SemanticType.TEXCOORD1_X;
        container["TEXCOORD1.Y"]        = SemanticType.TEXCOORD1_Y;
        container["TEXCOORD1.Z"]        = SemanticType.TEXCOORD1_Z;
        container["TEXCOORD1.W"]        = SemanticType.TEXCOORD1_W;
        container["TEXCOORD2.X"]        = SemanticType.TEXCOORD2_X;
        container["TEXCOORD2.Y"]        = SemanticType.TEXCOORD2_Y;
        container["TEXCOORD2.Z"]        = SemanticType.TEXCOORD2_Z;
        container["TEXCOORD2.W"]        = SemanticType.TEXCOORD2_W;
        container["TEXCOORD3.X"]        = SemanticType.TEXCOORD3_X;
        container["TEXCOORD3.Y"]        = SemanticType.TEXCOORD3_Y;
        container["TEXCOORD3.Z"]        = SemanticType.TEXCOORD3_Z;
        container["TEXCOORD3.W"]        = SemanticType.TEXCOORD3_W;
        container["TEXCOORD4.X"]        = SemanticType.TEXCOORD4_X;
        container["TEXCOORD4.Y"]        = SemanticType.TEXCOORD4_Y;
        container["TEXCOORD4.Z"]        = SemanticType.TEXCOORD4_Z;
        container["TEXCOORD4.W"]        = SemanticType.TEXCOORD4_W;
        container["TEXCOORD5.X"]        = SemanticType.TEXCOORD5_X;
        container["TEXCOORD5.Y"]        = SemanticType.TEXCOORD5_Y;
        container["TEXCOORD5.Z"]        = SemanticType.TEXCOORD5_Z;
        container["TEXCOORD5.W"]        = SemanticType.TEXCOORD5_W;
        container["TEXCOORD6.X"]        = SemanticType.TEXCOORD6_X;
        container["TEXCOORD6.Y"]        = SemanticType.TEXCOORD6_Y;
        container["TEXCOORD6.Z"]        = SemanticType.TEXCOORD6_Z;
        container["TEXCOORD6.W"]        = SemanticType.TEXCOORD6_W;
        container["TEXCOORD7.X"]        = SemanticType.TEXCOORD7_X;
        container["TEXCOORD7.Y"]        = SemanticType.TEXCOORD7_Y;
        container["TEXCOORD7.Z"]        = SemanticType.TEXCOORD7_Z;
        container["TEXCOORD7.W"]        = SemanticType.TEXCOORD7_W;
        container["COLOR0.X"]           = SemanticType.COLOR0_X;
        container["COLOR0.Y"]           = SemanticType.COLOR0_Y;
        container["COLOR0.Z"]           = SemanticType.COLOR0_Z;
        container["COLOR0.W"]           = SemanticType.COLOR0_W;
        container["COLOR.X"]            = SemanticType.COLOR0_X;
        container["COLOR.Y"]            = SemanticType.COLOR0_Y;
        container["COLOR.Z"]            = SemanticType.COLOR0_Z;
        container["COLOR.W"]            = SemanticType.COLOR0_W;
    }

    // jave.lin : 获取 parent transform 对象
    private Transform GetParentTrans()
    {
        var parentGO = GameObject.Find(GO_Parent_Name);
        if (parentGO == null)
        {
            parentGO = new GameObject(GO_Parent_Name);
            parentGO.transform.position = Vector3.zero;
            parentGO.transform.localRotation = Quaternion.identity;
            parentGO.transform.localScale = Vector3.one;
        }
        return parentGO.transform;
    }

    // jave.lin : 根据 CSV 内容生成 MeshRenderer 对应的 GO
    private GameObject GenerateGOWithMeshRendererFromCSV(List<string> vbv_csv_list, string ibv_csv, bool is_from_DX_CSV, Dictionary<Flip_UV_YCoordsType, bool> flipUVYDict)
    {
        var ret = new GameObject();

        var mesh = new Mesh();

        // jave.lin : 根据 csv 来填充 mesh 信息
        FillMeshFromCSV(mesh, vbv_csv_list, ibv_csv, is_from_DX_CSV, flipUVYDict);

        var meshFilter = ret.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        var meshRenderer = ret.AddComponent<MeshRenderer>();

        // jave.lin : 默认使用 URP 的 PBR Shader
        meshRenderer.sharedMaterial = material;

        ret.transform.position = Vector3.zero;
        ret.transform.localRotation = Quaternion.identity;
        ret.transform.localScale = Vector3.one;

        return ret;
    }

    // jave.lin : 根据 semantic type 和 data 来填充到 数据字段
    private void FillVertexFieldInfo(VertexInfo info, SemanticType semanticType, string data, bool is_from_DX_CSV, Dictionary<Flip_UV_YCoordsType, bool> flipUVYDict)
    {
        switch (semanticType)
        {
            // jave.lin : VTX
            case SemanticType.VTX:
                info.VTX = int.Parse(data);
                break;

            // jave.lin : IDX
            case SemanticType.IDX:
                info.IDX = int.Parse(data);
                break;

            // jave.lin : position
            case SemanticType.POSITION_X:
                ToValue(data, out info.POSITION_X);
                break;
            case SemanticType.POSITION_Y:
                ToValue(data, out info.POSITION_Y);
                break;
            case SemanticType.POSITION_Z:
                ToValue(data, out info.POSITION_Z);
                break;
            case SemanticType.POSITION_W:
                ToValue(data, out info.POSITION_W);
                Debug.LogWarning("WARNING: unity mesh cannot transfer position.w to shader program.");
                break;

            // jave.lin : normal
            case SemanticType.NORMAL_X:
                ToValue(data, out info.NORMAL_X);
                break;
            case SemanticType.NORMAL_Y:
                ToValue(data, out info.NORMAL_Y);
                break;
            case SemanticType.NORMAL_Z:
                ToValue(data, out info.NORMAL_Z);
                break;
            case SemanticType.NORMAL_W:
                ToValue(data, out info.NORMAL_W);
                break;

            // jave.lin : tangent
            case SemanticType.TANGENT_X:
                ToValue(data, out info.TANGENT_X);
                break;
            case SemanticType.TANGENT_Y:
                ToValue(data, out info.TANGENT_Y);
                break;
            case SemanticType.TANGENT_Z:
                ToValue(data, out info.TANGENT_Z);
                break;
            case SemanticType.TANGENT_W:
                ToValue(data, out info.TANGENT_W);
                break;

            // jave.lin : texcoord0
            case SemanticType.TEXCOORD0_X:
                ToValue(data, out info.TEXCOORD0_X);
                break;
            case SemanticType.TEXCOORD0_Y:
                {
                    ToValue(data, out info.TEXCOORD0_Y);
                    flipUVYDict.TryGetValue(Flip_UV_YCoordsType.TEXCOORD0_Y, out var flip);
                    if (flip && is_from_DX_CSV) info.TEXCOORD0_Y = 1 - info.TEXCOORD0_Y;
                }
                break;
            case SemanticType.TEXCOORD0_Z:
                ToValue(data, out info.TEXCOORD0_Z);
                break;
            case SemanticType.TEXCOORD0_W:
                ToValue(data, out info.TEXCOORD0_W);
                break;

            // jave.lin : texcoord1
            case SemanticType.TEXCOORD1_X:
                ToValue(data, out info.TEXCOORD1_X);
                break;
            case SemanticType.TEXCOORD1_Y:
                {
                    ToValue(data, out info.TEXCOORD1_Y);
                    flipUVYDict.TryGetValue(Flip_UV_YCoordsType.TEXCOORD1_Y, out var flip);
                    if (flip && is_from_DX_CSV) info.TEXCOORD1_Y = 1 - info.TEXCOORD1_Y;
                }
                break;
            case SemanticType.TEXCOORD1_Z:
                ToValue(data, out info.TEXCOORD1_Z);
                break;
            case SemanticType.TEXCOORD1_W:
                ToValue(data, out info.TEXCOORD1_W);
                break;

            // jave.lin : texcoord2
            case SemanticType.TEXCOORD2_X:
                ToValue(data, out info.TEXCOORD2_X);
                break;
            case SemanticType.TEXCOORD2_Y:
                {
                    ToValue(data, out info.TEXCOORD2_Y);
                    flipUVYDict.TryGetValue(Flip_UV_YCoordsType.TEXCOORD2_Y, out var flip);
                    if (flip && is_from_DX_CSV) info.TEXCOORD2_Y = 1 - info.TEXCOORD2_Y;
                }
                break;
            case SemanticType.TEXCOORD2_Z:
                ToValue(data, out info.TEXCOORD2_Z);
                break;
            case SemanticType.TEXCOORD2_W:
                ToValue(data, out info.TEXCOORD2_W);
                break;

            // jave.lin : texcoord3
            case SemanticType.TEXCOORD3_X:
                ToValue(data, out info.TEXCOORD3_X);
                break;
            case SemanticType.TEXCOORD3_Y:
                {
                    ToValue(data, out info.TEXCOORD3_Y);
                    flipUVYDict.TryGetValue(Flip_UV_YCoordsType.TEXCOORD3_Y, out var flip);
                    if (flip && is_from_DX_CSV) info.TEXCOORD3_Y = 1 - info.TEXCOORD3_Y;
                }
                break;
            case SemanticType.TEXCOORD3_Z:
                ToValue(data, out info.TEXCOORD3_Z);
                break;
            case SemanticType.TEXCOORD3_W:
                ToValue(data, out info.TEXCOORD3_W);
                break;

            // jave.lin : texcoord4
            case SemanticType.TEXCOORD4_X:
                ToValue(data, out info.TEXCOORD4_X);
                break;
            case SemanticType.TEXCOORD4_Y:
                {
                    ToValue(data, out info.TEXCOORD4_Y);
                    flipUVYDict.TryGetValue(Flip_UV_YCoordsType.TEXCOORD4_Y, out var flip);
                    if (flip && is_from_DX_CSV) info.TEXCOORD4_Y = 1 - info.TEXCOORD4_Y;
                }
                break;
            case SemanticType.TEXCOORD4_Z:
                ToValue(data, out info.TEXCOORD4_Z);
                break;
            case SemanticType.TEXCOORD4_W:
                ToValue(data, out info.TEXCOORD4_W);
                break;

            // jave.lin : texcoord5
            case SemanticType.TEXCOORD5_X:
                ToValue(data, out info.TEXCOORD5_X);
                break;
            case SemanticType.TEXCOORD5_Y:
                {
                    ToValue(data, out info.TEXCOORD5_Y);
                    flipUVYDict.TryGetValue(Flip_UV_YCoordsType.TEXCOORD5_Y, out var flip);
                    if (flip && is_from_DX_CSV) info.TEXCOORD5_Y = 1 - info.TEXCOORD5_Y;
                }
                break;
            case SemanticType.TEXCOORD5_Z:
                ToValue(data, out info.TEXCOORD5_Z);
                break;
            case SemanticType.TEXCOORD5_W:
                ToValue(data, out info.TEXCOORD5_W);
                break;

            // jave.lin : texcoord6
            case SemanticType.TEXCOORD6_X:
                ToValue(data, out info.TEXCOORD6_X);
                break;
            case SemanticType.TEXCOORD6_Y:
                {
                    ToValue(data, out info.TEXCOORD6_Y);
                    flipUVYDict.TryGetValue(Flip_UV_YCoordsType.TEXCOORD6_Y, out var flip);
                    if (flip && is_from_DX_CSV) info.TEXCOORD6_Y = 1 - info.TEXCOORD6_Y;
                }
                break;
            case SemanticType.TEXCOORD6_Z:
                ToValue(data, out info.TEXCOORD6_Z);
                break;
            case SemanticType.TEXCOORD6_W:
                ToValue(data, out info.TEXCOORD6_W);
                break;

            // jave.lin : texcoord7
            case SemanticType.TEXCOORD7_X:
                ToValue(data, out info.TEXCOORD7_X);
                break;
            case SemanticType.TEXCOORD7_Y:
                {
                    ToValue(data, out info.TEXCOORD7_Y);
                    flipUVYDict.TryGetValue(Flip_UV_YCoordsType.TEXCOORD7_Y, out var flip);
                    if (flip && is_from_DX_CSV) info.TEXCOORD7_Y = 1 - info.TEXCOORD7_Y;
                }
                break;
            case SemanticType.TEXCOORD7_Z:
                ToValue(data, out info.TEXCOORD7_Z);
                break;
            case SemanticType.TEXCOORD7_W:
                ToValue(data, out info.TEXCOORD7_W);
                break;

            // jave.lin : color0
            case SemanticType.COLOR0_X:
                ToValue(data, out info.COLOR0_X);
                break;
            case SemanticType.COLOR0_Y:
                ToValue(data, out info.COLOR0_Y);
                break;
            case SemanticType.COLOR0_Z:
                ToValue(data, out info.COLOR0_Z);
                break;
            case SemanticType.COLOR0_W:
                ToValue(data, out info.COLOR0_W);
                break;
            case SemanticType.Unknow:
                // jave.lin : nop
                break;
            // jave.lin : un-implements
            default:
                Debug.LogError($"Fill_A2V_Common_Type_Data un-implements SemanticType : {semanticType}");
                break;
        }
    }

    private void ToValue(string data, out float out_val) 
    {
        // jave.lin : GPA 导出来的数据有很多 infi 和 0 
        if (!float.TryParse(data, out out_val) ||
            float.IsNaN(out_val) || 
            float.IsInfinity(out_val) ||
            float.IsNegativeInfinity(out_val) ||
            float.IsPositiveInfinity(out_val)
            )
        {
            out_val = 0;
        }
    }

    //private void ToValue(string data, out int out_val)
    //{
    //    if (!int.TryParse(data, out out_val))
    //    {
    //        out_val = 0;
    //    }
    //}

    // jave.lin : 根据 nType 来处理 normalized
    private Vector4 NormalizedVec(Vector4 val, VBVChannelNormalizedType nType)
    {
        switch (nType)
        {
            case VBVChannelNormalizedType.None:
                return val;
            case VBVChannelNormalizedType.Nor_U8Bit:
                return new Vector4(((float)val.x) / byte.MaxValue, ((float)val.y) / byte.MaxValue, ((float)val.z) / byte.MaxValue, ((float)val.w) / byte.MaxValue);
            case VBVChannelNormalizedType.Nor_8Bit:
                return new Vector4(((float)val.x) / sbyte.MaxValue, ((float)val.y) / sbyte.MaxValue, ((float)val.z) / sbyte.MaxValue, ((float)val.w) / sbyte.MaxValue);
            case VBVChannelNormalizedType.Nor_U16Bit:
                return new Vector4(((float)val.x) / ushort.MaxValue, ((float)val.y) / ushort.MaxValue, ((float)val.z) / ushort.MaxValue, ((float)val.w) / ushort.MaxValue);
            case VBVChannelNormalizedType.Nor_16Bit:
                return new Vector4(((float)val.x) / short.MaxValue, ((float)val.y) / short.MaxValue, ((float)val.z) / short.MaxValue, ((float)val.w) / short.MaxValue);
            case VBVChannelNormalizedType.Nor_U32Bit:
                return new Vector4(((float)val.x) / uint.MaxValue, ((float)val.y) / uint.MaxValue, ((float)val.z) / uint.MaxValue, ((float)val.w) / uint.MaxValue);
            case VBVChannelNormalizedType.Nor_32Bit:
                return new Vector4(((float)val.x) / int.MaxValue, ((float)val.y) / int.MaxValue, ((float)val.z) / int.MaxValue, ((float)val.w) / int.MaxValue);
            default:
                return val;
        }
    }

    // jave.lin : 根据 csv 来填充 mesh 信息
    private void FillMeshFromCSV(Mesh mesh, List<string> vbv_csv_List, string ibv_csv, bool is_from_DX_CSV, Dictionary<Flip_UV_YCoordsType, bool> flipUVYDict)
    {
        var line_splitor = new string[] { "\n", "\r\n" };
        var line_element_splitor = new string[] { "," };

        // jave.lin : ibv 处理

        // jave.lin : 顶点数据
        var vertex_dict_key_idx = new Dictionary<int, VertexInfo>();
        // jave.lin : 索引数据
        var indices = new List<int>();
        {
            var lines = ibv_csv.Split(line_splitor, StringSplitOptions.RemoveEmptyEntries);
            // jave.lin : 跳过第一行，从第二行开始
            for (int i = 1; i < lines.Length; i++)
            {
                var linesElements = lines[i].Split(line_element_splitor, StringSplitOptions.RemoveEmptyEntries);
                // jave.lin : 直接取第二个数值作为索引
                indices.Add(int.Parse(linesElements[1]));
            }
        }

        // jave.lin : push padding or remove padding
        // jave.lin : 这么处理可以避免 https://blog.csdn.net/linjf520/article/details/127066726 文章中提及的 BUG：
        //  - Fail setting triangles. Some indices are referencing out of bounds vertices. IndexCount: xxx, VertexCount: xxx
        //  - Fail setting triangles. The number of supplied triangle indices must be a multiple of 3.
        if (indices.Count > 0 && indices.Count % 3 != 0)
        {
            var loopCount = 0;
            var lastOneVal = indices[indices.Count - 1];
            var lastOneIsZeroVal = lastOneVal == 0;
            while (indices.Count > 0 && indices.Count % 3 != 0)
            {
                if (loopCount > 10)
                    break;
                loopCount++;
                if (lastOneIsZeroVal)
                {
                    // jave.lin : remove padding 如果尾部是 0 索引，我们可以用删除 padding 的方式
                    indices.RemoveAt(indices.Count - 1);
                }
                else
                {
                    // jave.lin : push padding, 否则我们使用 push 最后一个顶点作为 padding 的方式
                    indices.Add(lastOneVal);
                }
            }
        }

        // jave.lin : 先映射好 semantics 名字和类型
        MappingSemanticsTypeByNames(ref semanticTypeDict_key_name_helper);
        var semantic_type_map_key_name = semanticTypeDict_key_name_helper;

        var semanticTitleListHelper = new List<SemanticType>();

        var remove_padding_title_regex = new Regex("(,\\w+\\d*p)", RegexOptions.Compiled);

        // jave.lin : vbv 处理
        foreach (var vbv_csv in vbv_csv_List)
        {
            var lines = vbv_csv.Split(line_splitor, StringSplitOptions.RemoveEmptyEntries);

            // jave.lin : lines[0] == "VTX, IDX, POSITION.x, POSITION.y, POSITION.z, NORMAL.x, NORMAL.y, NORMAL.z, NORMAL.w, TANGENT.x, TANGENT.y, TANGENT.z, TANGENT.w, TEXCOORD0.x, TEXCOORD0.y"

            // jave.lin : 构建 vertex buffer format 的 semantics 和 idx 的对应关系

            // jave.lin : 先删除 padding titles
            var firstLine = remove_padding_title_regex.Replace(lines[0], "");

            var semanticTitles = firstLine.Split(line_element_splitor, StringSplitOptions.RemoveEmptyEntries);
            semanticTitleListHelper.Clear();
            semanticTitleListHelper.Add(SemanticType.IDX);

            for (int i = 1; i < semanticTitles.Length; i++)
            {
                var semantic = semanticTitles[i].Trim().ToUpper();
                if (!semantic_type_map_key_name.TryGetValue(semantic, out SemanticType semanticType))
                {
                    semanticTypeDict_key_name_helper[semantic] = SemanticType.Unknow;
                    Debug.LogError($"Cannot find the semantic mapping data : {semantic}");
                }
                semanticTitleListHelper.Add(semanticType);
            }

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                var linesElements = line.Split(line_element_splitor, StringSplitOptions.RemoveEmptyEntries);

                var idx = int.Parse(linesElements[0]);
                // jave.lin : 如果该 vertex 没有处理过，那么才去处理
                if (!vertex_dict_key_idx.TryGetValue(idx, out VertexInfo info))
                {
                    info = new VertexInfo();
                    vertex_dict_key_idx[idx] = info;
                }
                // jave.lin : loop to fill the a2v field
                for (int j = 1; j < linesElements.Length; j++)
                {
                    FillVertexFieldInfo(info, semanticTitleListHelper[j], linesElements[j].Trim(), is_from_DX_CSV, flipUVYDict);
                }
            }
        }

        // jave.lin : 缩放、旋转、平移
        var rotation = Quaternion.Euler(vertexRotation);
        var TRS_mat = Matrix4x4.TRS(vertexOffset, rotation, vertexScale);
        // jave.lin : 法线变换矩阵需要特殊处理，针对 vertex scale 为非 uniform scale 的情况
        // ref : LearnGL - 11.5 - 矩阵04 - 法线从对象空间变换到世界空间
        // https://blog.csdn.net/linjf520/article/details/107501215
        var M_IT_mat = Matrix4x4.TRS(Vector3.zero, rotation, vertexScale).inverse.transpose;

        // jave.lin : composite the data （最后就是我们要组合数据，统一赋值给 mesh）
        var vertices = new Vector3[vertex_dict_key_idx.Count];
        var normals = new Vector3[vertex_dict_key_idx.Count];
        var tangents = new Vector4[vertex_dict_key_idx.Count];
        var uv = new Vector4[vertex_dict_key_idx.Count];
        var uv2 = new Vector4[vertex_dict_key_idx.Count];
        var uv3 = new Vector4[vertex_dict_key_idx.Count];
        var uv4 = new Vector4[vertex_dict_key_idx.Count];
        var uv5 = new Vector4[vertex_dict_key_idx.Count];
        var uv6 = new Vector4[vertex_dict_key_idx.Count];
        var uv7 = new Vector4[vertex_dict_key_idx.Count];
        var uv8 = new Vector4[vertex_dict_key_idx.Count];
        var color0 = new Color[vertex_dict_key_idx.Count];

        // jave.lin : 先拿到最大的index
        // 但是注意，indices 里面同样有可能有多余的数据，因此我们要看一下索引跨度很大的情况，删除后续无用的数据
        var indices_max_val = indices.Max() + 1;

        // jave.lin : 根据 0~count 的索引顺序来组织相关的 vertex 数据
        for (int idx = 0; idx < vertices.Length; idx++)
        {
            if (idx >= indices_max_val)
            {
                // jave.lin : GPA 抓出来的 CSV 有很多无用的数据
                Debug.LogWarning($"idx : {idx}, indices.Count:{indices.Count}, idx > indices.Count * 3");
                break;
            }
            var info = vertex_dict_key_idx[idx];
            
            vertices[idx] = TRS_mat * info.POSITION_H;

            // jave.lin : 解决 unity 提示： Invalid worldAABB.Object is too large or too far away from the origin
            // 这个原因是因为 GPA 里面的 VBV 复用了之前的 内存块，但是内存块没有重置大小，导致后续有多余的数据
            // 理论上，上面判断了： idx >= indices_max_val 就不会出现这里无用的异常数据
            if (Mathf.Abs(info.POSITION_X) > 1000 ||
                Mathf.Abs(info.POSITION_Y) > 1000 ||
                Mathf.Abs(info.POSITION_Z) > 1000)
            {
                // jave.lin : 有一些无用的数据的位置会异常，一般在判断了： idx >= indices.Count
                // 就不会有这个问题
                Debug.LogWarning($"idx : {idx}, position too large : {info.POSITION}");
            }
            normals[idx] = M_IT_mat * info.NORMAL;
            tangents[idx] = info.TANGENT;
            uv[idx] = info.TEXCOORD0;
            uv2[idx] = info.TEXCOORD1;
            uv3[idx] = info.TEXCOORD2;
            uv4[idx] = info.TEXCOORD3;
            uv5[idx] = info.TEXCOORD4;
            uv6[idx] = info.TEXCOORD5;
            uv7[idx] = info.TEXCOORD6;
            uv8[idx] = info.TEXCOORD7;
            color0[idx] = NormalizedVec(info.COLOR0, color0NormlizedType);
        }

        // jave.lin : 设置 mesh 信息
        mesh.vertices = vertices;

        // jave.lin : 是否 reverse idx
        if (is_reverse_vertex_order) indices.Reverse();
        mesh.triangles = indices.ToArray();

        // jave.lin : 测试一下，提前将 color 设置，看看会不会影响 后续的 uv 4v 设置的问题
        mesh.colors = include_color0 ? color0 : null;

        // jave.lin : unity 不能超过 uv[0~7]

        // jave.lin : 下面是旧版本的 set uv 方式，都是 vector2 的方式
        // 使用新版本的处理
        //mesh.uv = include_uv0 ? uv : null;
        //mesh.uv2 = include_uv1 ? uv2 : null;
        //mesh.uv3 = include_uv2 ? uv3 : null;
        //mesh.uv4 = include_uv3 ? uv4 : null;
        //mesh.uv5 = include_uv4 ? uv5 : null;
        //mesh.uv6 = include_uv5 ? uv6 : null;
        //mesh.uv7 = include_uv6 ? uv7 : null;
        //mesh.uv8 = include_uv7 ? uv8 : null;

        // jave.lin : 使用新版本的 vector4 来处理 uv 数据，这样分量就可以超过2个
        // 但是经过测试发现， Mesh.SetUVs(int channel, Vector4[] data) 也是没啥用
        // shader lab 中的 uv.xy 是正常的，但是uv.zw 的数据都是不正常的
        // 其实 unity mesh 中是OK的，但就是导出 FBX 后，只能保存 vector2 的 uv，unity FBX Exporter插件 的问题 
        if (include_uv0) mesh.SetUVs(0, uv);
        if (include_uv1) mesh.SetUVs(1, uv2);
        if (include_uv2) mesh.SetUVs(2, uv3);
        if (include_uv3) mesh.SetUVs(3, uv4);
        if (include_uv4) mesh.SetUVs(4, uv5);
        if (include_uv5) mesh.SetUVs(5, uv6);
        if (include_uv6) mesh.SetUVs(6, uv7);
        if (include_uv7) mesh.SetUVs(7, uv8);

        // jave.lin : AABB
        if (is_recalculate_bound)
        {
            mesh.RecalculateBounds();
        }

        // jave.lin : NORMAL
        switch (normalImportType)
        {
            case ModelImporterNormals.None:
                // nop
                break;
            case ModelImporterNormals.Import:
                mesh.normals = normals;
                break;
            case ModelImporterNormals.Calculate:
                mesh.RecalculateNormals();
                break;
            default:
                break;
        }

        // jave.lin : TANGENT
        switch (tangentImportType)
        {
            case ModelImporterTangents.None:
                // nop
                break;
            case ModelImporterTangents.Import:
                mesh.tangents = tangents;
                break;
            case ModelImporterTangents.CalculateLegacy:
            case ModelImporterTangents.CalculateLegacyWithSplitTangents:
            case ModelImporterTangents.CalculateMikk:
                mesh.RecalculateTangents();
                break;
            default:
                break;
        }

        //// jave.lin : 打印一下
        //Debug.Log("FillMeshFromCSV done!");
    }
}