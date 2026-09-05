// TextField.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using UnityEngine;
using Verse;

namespace ColourPicker
{
    public class TextField<T>

    {
    private T _value;
    private string _id;
    private string _temp;
    private Func<string, bool> _validator;
    private Func<string, T> _parser;
    private Func<T, string> _toString;
    private Action<T> _callback;
    private bool _spinner;

    //The validator parses the text; it only needs to run when the text changes.
    private bool _valid = true;
    private string _validatedFor;

    public TextField( T value, string id, Action<T> callback, Func<string, T> parser = null,
        Func<string, bool> validator = null, Func<T, string> toString = null, bool spinner = false )
    {
        _value = value;
        _id = id;
        _temp = value.ToString();
        _callback = callback;
        _validator = validator;
        _parser = parser;
        _toString = toString;
        _spinner = spinner;
    }

    public T Value
    {
        get => _value;
        set
        {
            _value = value;
            _temp = _toString?.Invoke( value ) ?? value.ToString();
        }
    }

    public static TextField<float> Float01( float value, string id, Action<float> callback )
    {
        return new TextField<float>( value, id, callback, float.Parse, Validate01, f => Round( f ).ToString(), true );
    }

    public static TextField<string> Hex( string value, string id, Action<string> callback )
    {
        return new TextField<string>( value, id, callback, hex => hex, ValidateHex );
    }

    private bool IsValid()
    {
        if ( !ReferenceEquals( _validatedFor, _temp ) )
        {
            _validatedFor = _temp;
            _valid = _validator?.Invoke( _temp ) ?? true;
        }

        return _valid;
    }

    public void Draw( Rect rect )
    {
        GUI.color = IsValid() ? Color.white : Color.red;
        GUI.SetNextControlName( _id );
        var temp = Widgets.TextField( rect, _temp );
        GUI.color = Color.white;

        if ( temp != _temp )
        {
            _temp = temp;
            if ( IsValid() )
            {
                _value = _parser( _temp );
                _callback?.Invoke( _value );
            }
        }
    }

    private static bool Validate01( string value )
    {
        if ( !float.TryParse( value, out var parsed ) )
            return false;
        return parsed >= 0f && parsed <= 1f;
    }

    private static bool ValidateHex( string value )
    {
        return ColorUtility.TryParseHtmlString( value, out var color );
    }

    private static float Round( float value, int digits = 2 )
    {
        var exponent = Mathf.Pow( 10, digits );
        return Mathf.RoundToInt( value * exponent ) / exponent;
    }
    }
}