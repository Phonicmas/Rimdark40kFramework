using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ColourPicker
{
    public class Dialog_ColourPicker : Window
    {
        enum controls
        {
            colourPicker,
            huePicker,
            alphaPicker,
            none
        }

        private controls _activeControl = controls.none;

        private Texture2D _colourPickerBG,
            _huePickerBG,
            _alphaPickerBG,
            _tempPreviewBG,
            _previewBG,
            _pickerAlphaBG,
            _sliderAlphaBG,
            _previewAlphaBG;

        private Color _alphaBGColorA = Color.white,
            _alphaBGColorB = new Color( .85f, .85f, .85f );

        private int _pickerSize = 300,
            _sliderWidth = 15,
            _alphaBGBlockSize = 10,
            _previewSize = 90, // odd multiple of alphaBGblocksize forces alternation of the background texture grid.
            _handleSize = 10,
            _recentSize = 20;

        private float _margin = 6f,
            _buttonHeight = 30f,
            _fieldHeight = 24f,
            _huePosition,
            _alphaPosition,
            _unitsPerPixel,
            _h,
            _s,
            _v;

        private List<string> textFieldIds;

        public bool autoApply = false;
        public bool minimalistic = false;

        private TextField<float> RedField, GreenField, BlueField, HueField, SaturationField, ValueField, Alpha1Field, Alpha2Field;
        private TextField<string> HexField;

        private string _hex;

        private RecentColours _recentColours = new RecentColours();

        public string Hex
        {
            get => $"#{ColorUtility.ToHtmlStringRGBA( tempColour )}";
            set
            {
                _hex = value;
                NotifyHexUpdated();
            }
        }

        private Vector2 _position = Vector2.zero;

        private Action<Color> _callback;

        // the colour we're going to pass out if requested
        public Color curColour;

        // used in the picker only
        private Color _tempColour;

        public Color tempColour
        {
            get => _tempColour;
            set
            {
                _tempColour = value;
                if (autoApply || minimalistic)
                    SetColor();
            }
        }

        private Vector2? _initialPosition;

        public Vector2 InitialPosition => _initialPosition ??
                                          new Vector2( UI.screenWidth - InitialSize.x,
                                              UI.screenHeight - InitialSize.y ) / 2f;

        /// <summary>
        /// Call with the current colour, and a callback which will be passed the new colour when 'OK' or 'Apply' is pressed. Optionally, the colour pickers' position can be provided.
        /// </summary>
        /// <param name="color">The current colour</param>
        /// <param name="callback">Callback to be invoked with the selected colour when 'OK' or 'Apply' are pressed'</param>
        /// <param name="position">Top left position of the colour picker (defaults to screen center)</param>
        public Dialog_ColourPicker( Color color, Action<Color> callback = null, Vector2? position = null )
        {
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;

            _callback = callback;
            _initialPosition = position;

            curColour = color;
            tempColour = color;

            HueField = TextField<float>.Float01(H, "Hue", h => H = h);
            SaturationField = TextField<float>.Float01(S, "Saturation", s => S = s);
            ValueField = TextField<float>.Float01(V, "Value", v => V = v);
            Alpha1Field = TextField<float>.Float01(A, "Alpha1", a => A = a);
            RedField = TextField<float>.Float01( color.r, "Red", r => R = r );
            GreenField = TextField<float>.Float01( color.r, "Green", g => G = g );
            BlueField = TextField<float>.Float01( color.r, "Blue", b => B = b );
            Alpha2Field = TextField<float>.Float01(A, "Alpha2", a => A = a);
            HexField = TextField<string>.Hex( Hex, "Hex", hex => Hex = hex );

            textFieldIds = new List<string>([
                "Hue", "Saturation", "Value", "Alpha1", "Red", "Green", "Blue", "Alpha2", "Hex"
            ]);

            NotifyRGBUpdated();
        }

        public float UnitsPerPixel
        {
            get
            {
                if ( _unitsPerPixel == 0.0f )
                {
                    _unitsPerPixel = 1f / _pickerSize;
                }
                return _unitsPerPixel;
            }
        }

        public float H
        {
            get => _h;
            set
            {
                _h = Mathf.Clamp( value, 0f, 1f );
                NotifyHSVUpdated();
                CreateColourPickerBG();
                CreateAlphaPickerBG();
            }
        }

        public float S
        {
            get => _s;
            set
            {
                _s = Mathf.Clamp( value, 0f, 1f );
                NotifyHSVUpdated();
                CreateAlphaPickerBG();
            }
        }

        public float V
        {
            get => _v;
            set
            {
                _v = Mathf.Clamp( value, 0f, 1f );
                NotifyHSVUpdated();
                CreateAlphaPickerBG();
            }
        }

        public float A
        {
            get => tempColour.a;
            set
            {
                var color = tempColour;
                color.a = Mathf.Clamp(value, 0f, 1f);
                tempColour = color;
                NotifyRGBUpdated();
            }
        }
        public float R
        {
            get => tempColour.r;
            set
            {
                var color = tempColour;
                color.r = Mathf.Clamp(value, 0f, 1f);
                tempColour = color;
                NotifyRGBUpdated();
            }
        }
        public float G
        {
            get => tempColour.g;
            set
            {
                var color = tempColour;
                color.g = Mathf.Clamp(value, 0f, 1f);
                tempColour = color;
                NotifyRGBUpdated();
            }
        }
        public float B
        {
            get => tempColour.b;
            set
            {
                var color = tempColour;
                color.b = Mathf.Clamp(value, 0f, 1f);
                tempColour = color;
                NotifyRGBUpdated();
            }
        }

        public void SetPickerPositions()
        {
            // set slider positions
            _huePosition = (1f - H) / UnitsPerPixel;
            _position.x = S / UnitsPerPixel;
            _position.y = (1f - V) / UnitsPerPixel;
            _alphaPosition = (1f - A) / UnitsPerPixel;
        }

        public void NotifyHSVUpdated()
        {
           //Debug( $"HSV updated: ({_h}, {_s}, {_v})" );

            // update rgb colour 
            var color = Color.HSVToRGB( H, S, V );
            color.a = A;
            tempColour = color;

            // set the colour block
            CreatePreviewBG( ref _tempPreviewBG, tempColour );
            SetPickerPositions();

            // update text fields
            RedField.Value = tempColour.r;
            GreenField.Value = tempColour.g;
            BlueField.Value = tempColour.b;
            HueField.Value = H;
            SaturationField.Value = S;
            ValueField.Value = V;
            Alpha1Field.Value = A;
            Alpha2Field.Value = A;
            HexField.Value = Hex;
        }

        public void NotifyRGBUpdated()
        {
            //Debug($"RGB updated: ({R}, {G}, {B})");

            // Set HSV from RGB
            Color.RGBToHSV( tempColour, out _h, out _s, out _v );

            // rebuild textures
            CreateColourPickerBG();
            CreateHuePickerBG();
            CreateAlphaPickerBG();

            // set the colour block
            CreatePreviewBG( ref _tempPreviewBG, tempColour );
            SetPickerPositions();

            // udpate text fields
            HueField.Value = H;
            SaturationField.Value = S;
            ValueField.Value = V;
            Alpha1Field.Value = A;
            Alpha2Field.Value = A;
            HexField.Value = Hex;
        }

        public void NotifyHexUpdated()
        {
            //Debug( $"HEX updated ({Hex})");

            if (ColorUtility.TryParseHtmlString( _hex, out var color))
            {
                // set rgb colour;
                tempColour = color;

                // do all the rgb update actions
                NotifyRGBUpdated();

                // also set RGB text fields
                RedField.Value = tempColour.r;
                GreenField.Value = tempColour.g;
                BlueField.Value = tempColour.b;
            }
        }

        public void SetColor()
        {
            curColour = tempColour;
            _recentColours.Add( tempColour );
            _callback?.Invoke( curColour );
            CreatePreviewBG( ref _previewBG, tempColour );
        }

        public Texture2D ColourPickerBG
        {
            get
            {
                if ( _colourPickerBG == null )
                {
                    CreateColourPickerBG();
                }
                return _colourPickerBG;
            }
        }

        public Texture2D HuePickerBG
        {
            get
            {
                if ( _huePickerBG == null )
                {
                    CreateHuePickerBG();
                }
                return _huePickerBG;
            }
        }

        public Texture2D AlphaPickerBG
        {
            get
            {
                if ( _alphaPickerBG == null )
                {
                    CreateAlphaPickerBG();
                }
                return _alphaPickerBG;
            }
        }

        public Texture2D TempPreviewBG
        {
            get
            {
                if ( _tempPreviewBG == null )
                {
                    CreatePreviewBG( ref _tempPreviewBG, tempColour );
                }
                return _tempPreviewBG;
            }
        }

        public Texture2D PreviewBG
        {
            get
            {
                if ( _previewBG == null )
                {
                    CreatePreviewBG( ref _previewBG, curColour );
                }
                return _previewBG;
            }
        }

        public Texture2D PickerAlphaBG
        {
            get
            {
                if ( _pickerAlphaBG == null )
                {
                    CreateAlphaBG( ref _pickerAlphaBG, _pickerSize, _pickerSize );
                }
                return _pickerAlphaBG;
            }
        }


        public Texture2D SliderAlphaBG
        {
            get
            {
                if ( _sliderAlphaBG == null )
                {
                    CreateAlphaBG( ref _sliderAlphaBG, _sliderWidth, _pickerSize );
                }
                return _sliderAlphaBG;
            }
        }

        public Texture2D PreviewAlphaBG
        {
            get
            {
                if ( _previewAlphaBG == null )
                {
                    CreateAlphaBG( ref _previewAlphaBG, _previewSize, _previewSize );
                }
                return _previewAlphaBG;
            }
        }

        private void SwapTexture( ref Texture2D tex, Texture2D newTex )
        {
            Object.Destroy( tex );
            tex = newTex;
        }

        private void CreateColourPickerBG()
        {
            float S, V;
            int w = _pickerSize;
            int h = _pickerSize;
            float wu = UnitsPerPixel;
            float hu = UnitsPerPixel;

            Texture2D tex = new Texture2D( w, h );

            // HSV colours, H in slider, S horizontal, V vertical.
            for ( int x = 0; x < w; x++ )
            {
                for ( int y = 0; y < h; y++ )
                {
                    S = x * wu;
                    V = y * hu;
                    tex.SetPixel( x, y, HSVAToRGB( H, S, V, A ) );
                }
            }
            tex.Apply();

            SwapTexture( ref _colourPickerBG, tex );
        }

        private void CreateHuePickerBG()
        {
            Texture2D tex = new Texture2D( 1, _pickerSize );

            var h = _pickerSize;
            var hu = 1f / h;

            // HSV colours, S = V = 1
            for ( int y = 0; y < h; y++ )
            {
                tex.SetPixel( 0, y, Color.HSVToRGB( hu * y, 1f, 1f ) );
            }
            tex.Apply();

            SwapTexture( ref _huePickerBG, tex );
        }

        private void CreateAlphaPickerBG()
        {
            Texture2D tex = new Texture2D( 1, _pickerSize );

            var h = _pickerSize;
            var hu = 1f / h;

            // RGB color from cache, alternate a
            for ( int y = 0; y < h; y++ )
            {
                tex.SetPixel( 0, y, new Color( tempColour.r, tempColour.g, tempColour.b, y * hu ) );
            }
            tex.Apply();

            SwapTexture( ref _alphaPickerBG, tex );
        }

        private void CreateAlphaBG( ref Texture2D bg, int width, int height )
        {
            Texture2D tex = new Texture2D( width + _alphaBGBlockSize, height + _alphaBGBlockSize);

            // initialize color arrays for blocks
            Color[] bgA = new Color[_alphaBGBlockSize * _alphaBGBlockSize];
            for ( int i = 0; i < bgA.Length; i++ ) bgA[i] = _alphaBGColorA;
            Color[] bgB = new Color[_alphaBGBlockSize * _alphaBGBlockSize];
            for ( int i = 0; i < bgB.Length; i++ ) bgB[i] = _alphaBGColorB;

            // set blocks of pixels at a time
            // this also sets border blocks, meaning it'll try to set out of bounds pixels. 
            int row = 0;
            for ( int x = 0; x < width; x = x + _alphaBGBlockSize )
            {
                int column = row;
                for ( int y = 0; y < height; y = y + _alphaBGBlockSize )
                {
                    tex.SetPixels( x, y, _alphaBGBlockSize, _alphaBGBlockSize, ( column % 2 == 0 ? bgA : bgB ) );
                    column++;
                }
                row++;
            }

            tex.Apply();
            SwapTexture( ref bg, tex );
        }

        public void CreatePreviewBG( ref Texture2D bg, Color col )
        {
            SwapTexture( ref bg, SolidColorMaterials.NewSolidColorTexture( col ) );
        }

        public void PickerAction( Vector2 pos )
        {
            // if we set S, V via properties these will be called twice. 
            _s = UnitsPerPixel * pos.x;
            _v = 1 - UnitsPerPixel * pos.y;

            CreateAlphaPickerBG();
            NotifyHSVUpdated();
            _position = pos;
        }

        public void HueAction( float pos )
        {
            // only changing one value, property should work fine
            H = 1 - UnitsPerPixel * pos;
            _huePosition = pos;
        }

        public void AlphaAction( float pos )
        {
            // only changing one value, property should work fine
            A = 1 - UnitsPerPixel * pos;
            _alphaPosition = pos;
        }

        protected override void SetInitialSizeAndPosition()
        {
            // get position based on requested size and position, limited by screen space.
            var size = new Vector2(
                Mathf.Min( InitialSize.x, UI.screenWidth ),
                Mathf.Min( InitialSize.y, UI.screenHeight - 35f ) );

            var position = new Vector2(
                Mathf.Max( 0f, Mathf.Min( InitialPosition.x, UI.screenWidth - size.x ) ),
                Mathf.Max( 0f, Mathf.Min( InitialPosition.y, UI.screenHeight - size.y ) ) );

            windowRect = new Rect( position.x, position.y, size.x, size.y );
        }

        public override void PreOpen()
        {
            base.PreOpen();
            NotifyHSVUpdated();
        }
        
        public override void DoWindowContents( Rect inRect )
        {
            // set up rects
            Rect pickerRect = new Rect( inRect.xMin, inRect.yMin, _pickerSize, _pickerSize );
            Rect hueRect = new Rect( pickerRect.xMax + _margin, inRect.yMin, _sliderWidth, _pickerSize );
            Rect alphaRect = new Rect( hueRect.xMax + _margin, inRect.yMin, _sliderWidth, _pickerSize );
            Rect previewRect = new Rect( alphaRect.xMax + _margin, inRect.yMin, _previewSize, _previewSize );
            Rect previewOldRect = new Rect( previewRect.xMax, inRect.yMin, _previewSize, _previewSize );
            Rect doneRect = new Rect( alphaRect.xMax + _margin, inRect.yMax - _buttonHeight, _previewSize * 2,
                _buttonHeight );
            Rect setRect = new Rect( alphaRect.xMax + _margin, inRect.yMax - 2 * _buttonHeight - _margin,
                _previewSize - _margin / 2, _buttonHeight );
            Rect cancelRect = new Rect( setRect.xMax + _margin, setRect.yMin, _previewSize - _margin / 2,
                _buttonHeight );
            Rect hsvFieldRect = new Rect( alphaRect.xMax + _margin, inRect.yMax - 2 * _buttonHeight - 3 * _fieldHeight - 4 * _margin,
                _previewSize * 2, _fieldHeight );
            Rect rgbFieldRect = new Rect( alphaRect.xMax + _margin, inRect.yMax - 2 * _buttonHeight - 2 * _fieldHeight - 3 * _margin,
                _previewSize * 2, _fieldHeight );
            Rect hexRect = new Rect( alphaRect.xMax + _margin, inRect.yMax - 2 * _buttonHeight - 1 * _fieldHeight - 2 * _margin,
                _previewSize * 2, _fieldHeight );
            Rect recentRect = new Rect( previewRect.xMin, previewRect.yMax + _margin, _previewSize * 2,
                _recentSize * 2 );

            // draw transparency backgrounds
            GUI.DrawTexture( pickerRect, PickerAlphaBG );
            GUI.DrawTexture( alphaRect, SliderAlphaBG );
            GUI.DrawTexture( previewRect, PreviewAlphaBG );
            GUI.DrawTexture( previewOldRect, PreviewAlphaBG );

            // draw picker foregrounds
            GUI.DrawTexture( pickerRect, ColourPickerBG );
            GUI.DrawTexture( hueRect, HuePickerBG );
            GUI.DrawTexture( alphaRect, AlphaPickerBG );
            GUI.DrawTexture( previewRect, TempPreviewBG );
            GUI.DrawTexture( previewOldRect, PreviewBG );

            if ( Widgets.ButtonInvisible( previewOldRect ) )
            {
                tempColour = curColour;
                NotifyRGBUpdated();
            }

            // draw recent colours
            DrawRecent( recentRect );

            // draw slider handles
            // TODO: get HSV from RGB for init of handles.
            Rect hueHandleRect = new Rect( hueRect.xMin - 3f, hueRect.yMin + _huePosition - _handleSize / 2,
                _sliderWidth + 6f, _handleSize );
            Rect alphaHandleRect = new Rect( alphaRect.xMin - 3f, alphaRect.yMin + _alphaPosition - _handleSize / 2,
                _sliderWidth + 6f, _handleSize );
            Rect pickerHandleRect = new Rect( pickerRect.xMin + _position.x - _handleSize / 2,
                pickerRect.yMin + _position.y - _handleSize / 2, _handleSize, _handleSize );
            GUI.DrawTexture( hueHandleRect, TempPreviewBG );
            GUI.DrawTexture( alphaHandleRect, TempPreviewBG );
            GUI.DrawTexture( pickerHandleRect, TempPreviewBG );

            GUI.color = Color.gray;
            Widgets.DrawBox( hueHandleRect );
            Widgets.DrawBox( alphaHandleRect );
            Widgets.DrawBox( pickerHandleRect );
            GUI.color = Color.white;

            // reset active control on mouseup
            if (Input.GetMouseButtonUp(0))
            {
                _activeControl = controls.none;
            }

            DrawColourPicker( pickerRect );
            DrawHuePicker( hueRect );
            DrawAlphaPicker( alphaRect );
            DrawFields( hsvFieldRect, rgbFieldRect, hexRect );
            DrawButtons( doneRect, setRect, cancelRect );

            GUI.color = Color.white;
        }

        private void DrawRecent( Rect canvas )
        {
            var cols = (int)(canvas.width / _recentSize);
            var rows = (int)(canvas.height / _recentSize);
            var n = Math.Min( cols * rows, _recentColours.Count );

            GUI.BeginGroup( canvas );
            for ( int i = 0; i < n; i++ )
            {
                var col = i % cols;
                var row = i / cols;
                var color = _recentColours[i];
                var rect = new Rect( col * _recentSize, row * _recentSize, _recentSize, _recentSize );
                Widgets.DrawBoxSolid( rect, color );
                if ( Mouse.IsOver( rect ) )
                    Widgets.DrawBox( rect );
                if ( Widgets.ButtonInvisible( rect ) )
                {
                    tempColour = color;
                    NotifyRGBUpdated();
                }
            }
            GUI.EndGroup();
        }

        private void DrawAlphaPicker( Rect alphaRect )
        {
// alpha picker interaction
            if ( Mouse.IsOver( alphaRect ) )
            {
                if ( Input.GetMouseButtonDown( 0 ) )
                {
                    _activeControl = controls.alphaPicker;
                }
                if ( Event.current.type == EventType.ScrollWheel )
                {
                    A -= Event.current.delta.y * UnitsPerPixel;
                    _alphaPosition = Mathf.Clamp( _alphaPosition + Event.current.delta.y, 0f, _pickerSize );
                    Event.current.Use();
                }
                if ( _activeControl == controls.alphaPicker )
                {
                    float MousePosition = Event.current.mousePosition.y;
                    float PositionInRect = MousePosition - alphaRect.yMin;

                    AlphaAction( PositionInRect );
                }
            }
        }

        private void DrawHuePicker( Rect hueRect )
        {
// hue picker interaction
            if ( Mouse.IsOver( hueRect ) )
            {
                if ( Input.GetMouseButtonDown( 0 ) )
                {
                    _activeControl = controls.huePicker;
                }
                if ( Event.current.type == EventType.ScrollWheel )
                {
                    H -= Event.current.delta.y * UnitsPerPixel;
                    _huePosition = Mathf.Clamp( _huePosition + Event.current.delta.y, 0f, _pickerSize );
                    Event.current.Use();
                }
                if ( _activeControl == controls.huePicker )
                {
                    float MousePosition = Event.current.mousePosition.y;
                    float PositionInRect = MousePosition - hueRect.yMin;

                    HueAction( PositionInRect );
                }
            }
        }

        private void DrawColourPicker( Rect pickerRect )
        {
// colourpicker interaction
            if ( Mouse.IsOver( pickerRect ) )
            {
                if ( Input.GetMouseButtonDown( 0 ) )
                {
                    _activeControl = controls.colourPicker;
                }
                if ( _activeControl == controls.colourPicker )
                {
                    Vector2 MousePosition = Event.current.mousePosition;
                    Vector2 PositionInRect = MousePosition - new Vector2( pickerRect.xMin, pickerRect.yMin );

                    PickerAction( PositionInRect );
                }
            }
        }

        private void DrawButtons( Rect doneRect, Rect setRect, Rect cancelRect )
        {
            if ( Widgets.ButtonText( doneRect, "OK" ) )
            {
                SetColor();
                Close();
            }
            if ( Widgets.ButtonText( setRect, "Apply" ) )
            {
                SetColor();
            }
            if ( Widgets.ButtonText( cancelRect, "Cancel" ) )
            {
                Close();
            }
        }

        private void DrawFields( Rect hsvFieldRect, Rect rgbFieldRect, Rect hexRect )
        {
            Text.Font = GameFont.Small;

            var fieldRect = hsvFieldRect;
            fieldRect.width /= 5f;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.grey;
            Widgets.Label( fieldRect, "HSV" );
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            fieldRect.x += fieldRect.width;
            HueField.Draw( fieldRect );
            fieldRect.x += fieldRect.width;
            SaturationField.Draw( fieldRect );
            fieldRect.x += fieldRect.width;
            ValueField.Draw( fieldRect );
            fieldRect.x += fieldRect.width;
            Alpha1Field.Draw( fieldRect );

            fieldRect = rgbFieldRect;
            fieldRect.width /= 5f;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.grey;
            Widgets.Label( fieldRect, "RGB" );
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            fieldRect.x += fieldRect.width;
            RedField.Draw( fieldRect );
            fieldRect.x += fieldRect.width;
            GreenField.Draw( fieldRect );
            fieldRect.x += fieldRect.width;
            BlueField.Draw( fieldRect );
            fieldRect.x += fieldRect.width;
            Alpha2Field.Draw( fieldRect );

            Text.Font = GameFont.Tiny;
            GUI.color = Color.grey;
            Widgets.Label( new Rect( hexRect.xMin, hexRect.yMin, fieldRect.width, hexRect.height ), "HEX" );
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            hexRect.xMin += fieldRect.width;
            HexField.Draw( hexRect );
            Text.Anchor = TextAnchor.UpperLeft;

            if ( Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab )
            {
                var curControl = GUI.GetNameOfFocusedControl();
                var curControlIndex = textFieldIds.IndexOf( curControl );
                GUI.FocusControl( textFieldIds[
                    GenMath.PositiveMod( curControlIndex + ( Event.current.shift ? -1 : 1 ), textFieldIds.Count )] );
            }
        }

        public override Vector2 InitialSize
        {
            get
            {
                // calculate window size to accomodate all elements
                return new Vector2(
                    _pickerSize + 3 * _margin + 2 * _sliderWidth + 2 * _previewSize + StandardMargin * 2,
                    _pickerSize + StandardMargin * 2 );
            }
        }

        public static Color HSVAToRGB( float H, float S, float V, float A )
        {
            var color = Color.HSVToRGB( H, S, V );
            color.a = A;
            return color;
        }

        public override void OnAcceptKeyPressed()
        {
            base.OnAcceptKeyPressed();
            SetColor();
        }
    }
}