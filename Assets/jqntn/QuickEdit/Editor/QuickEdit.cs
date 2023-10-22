#nullable enable

using System;
using System.IO;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using ColorUtility = UnityEngine.ColorUtility;

namespace jqntn.QuickEdit
{
    [CustomEditor(typeof(TextAsset), true)] internal class QE_TextAsset : QuickEdit { }
    [CustomEditor(typeof(MonoScript), true)] internal class QE_MonoScript : QuickEdit { }
    [CustomEditor(typeof(Shader), true)] internal class QE_Shader : QuickEdit { }

    [CustomEditor(typeof(DefaultAsset), true)]
    internal class QuickEdit : Editor
    {
        private const string LIGHT_RED = "#EE9090";
        private const string LIGHT_GREEN = "#90EE90";
        private const string TEAL_BLUE = "#00CCCC";
        private const string MAROON_RED = "#FF8080";

        private const string PLUGIN_PREFIX = "Quick";
        private const string EDIT_BUTTON_LABEL = "Edit";
        private const string CONFIRM_BUTTON_LABEL = "Apply";
        private const string CANCEL_BUTTON_LABEL = "Revert";
        private const string LARGE_FILE_WARNING = "(editing a large file may cause slowdown)";
        private const string DEFAULT_ENCODING = "iso-8859-1";
        private const string HEADER_STYLE = "In BigTitle";
        private const int MAX_FILE_SIZE = 1024 * 1024;
        private const int ICON_SIZE = 42;

        private readonly string _editorNamespace = typeof(Editor).Namespace;
        private readonly bool useMinimalUI = true;

        private Color EditButtonColor => EditorGUIUtility.isProSkin ? Color.cyan : GetColor(TEAL_BLUE);
        private Color ConfirmButtonColor => EditorGUIUtility.isProSkin ? GetColor(LIGHT_RED) : GetColor(MAROON_RED);
        private Color CancelButtonColor => GUI.color;
        private Color WarningLabelColor => Color.yellow;

        private Editor? _defaultEditor;
        private Texture? _icon;
        private Encoding? _encoding;
        private Color _defaultBackgroundColor;
        private Color _defaultContentColor;
        private string _assetPath = string.Empty;
        private string _text = string.Empty;
        private bool _isFileTooLarge;
        private bool _isFolder;
        private bool _canEdit;

        private void OnEnable()
        {
            _assetPath = AssetDatabase.GetAssetPath(target);

            if (AssetDatabase.IsValidFolder(_assetPath))
            {
                _isFolder = true;
                return;
            }

            var targetType = $"{target.GetType().Name}{nameof(Inspector)}";
            var editorType = Type.GetType($"{_editorNamespace}.{targetType}, {_editorNamespace}");

            if (editorType is not null) _defaultEditor = CreateEditor(target, editorType);

            _icon = Resources.Load<Texture>($"jqntn/QuickEdit/icon_{(EditorGUIUtility.isProSkin ? "light" : "dark")}");
            _encoding = DetectFileEncoding(_assetPath);
            _defaultBackgroundColor = GUI.backgroundColor;
            _defaultContentColor = GUI.contentColor;
            _isFileTooLarge = new FileInfo(_assetPath).Length >= MAX_FILE_SIZE;
        }

        private void OnDisable()
        {
            if (_isFolder) return;

            DestroyImmediate(_defaultEditor);
        }

        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();

            if (_isFolder) return;

            DrawQuickEdit();
        }

        public override void OnInspectorGUI()
        {
            if (_defaultEditor == null)
                base.OnInspectorGUI();
            else if (_isFolder || !_canEdit)
                _defaultEditor.OnInspectorGUI();
        }

        private void DrawQuickEdit()
        {
            var style = new GUIStyle();

            if (target.GetType() != typeof(DefaultAsset))
            {
                EditorGUILayout.Space(sizeof(int) * sizeof(int));
                style = GUI.skin.FindStyle(HEADER_STYLE);
            }

            EditorGUILayout.BeginVertical(style);

            if (!useMinimalUI)
                GUILayout.Label(_icon, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fixedHeight = ICON_SIZE });

            using (new EditorGUILayout.HorizontalScope())
            {
                if (_canEdit)
                {
                    GUI.backgroundColor = CancelButtonColor;
                    if (GUILayout.Button(EditorGUIUtility.TrTextContent(CANCEL_BUTTON_LABEL), EditorStyles.miniButtonLeft)) _canEdit = false;
                }
                else
                {
                    var editButton = false;
                    if (useMinimalUI)
                    {
                        using var _ = new EditorGUILayout.HorizontalScope();
                        GUILayout.FlexibleSpace();
                        editButton = GUILayout.Button(EditorGUIUtility.TrTextContent(EDIT_BUTTON_LABEL), new GUIStyle(EditorStyles.miniButton) { margin = new RectOffset() { right = sizeof(int) } });
                    }
                    else
                    {
                        GUI.backgroundColor = EditButtonColor;
                        editButton = GUILayout.Button($"{EditorGUIUtility.TrTextContent(PLUGIN_PREFIX)} {EditorGUIUtility.TrTextContent(EDIT_BUTTON_LABEL)}");
                    }
                    if (editButton)
                    {
                        GUI.FocusControl(null);
                        _text = File.ReadAllText(_assetPath, _encoding);
                        _canEdit = true;
                    }
                }
                GUI.backgroundColor = ConfirmButtonColor;
                if (_canEdit && GUILayout.Button(EditorGUIUtility.TrTextContent(CONFIRM_BUTTON_LABEL), EditorStyles.miniButtonRight))
                {
                    File.WriteAllText(_assetPath, _text, _encoding);
                    AssetDatabase.Refresh();
                    _canEdit = false;
                }
                GUI.backgroundColor = _defaultBackgroundColor;
            }

            if (!useMinimalUI && !_canEdit && _isFileTooLarge)
            {
                GUI.contentColor = WarningLabelColor;
                EditorGUILayout.LabelField(LARGE_FILE_WARNING, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                GUI.contentColor = _defaultContentColor;
            }

            if (_canEdit)
                _text = EditorGUILayout.TextArea(_text);

            EditorGUILayout.EndVertical();
        }

        private Encoding DetectFileEncoding(string filePath)
        {
            var encodings = new Encoding[] { new UTF8Encoding(false, true) };
            foreach (var encoding in encodings)
            {
                using var reader = new StreamReader(filePath, encoding);
                try { reader.Peek(); }
                catch { continue; }
                return reader.CurrentEncoding;
            }
            try { return Encoding.GetEncoding(DEFAULT_ENCODING); }
            catch { return Encoding.Default; }
        }

        private Color GetColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}