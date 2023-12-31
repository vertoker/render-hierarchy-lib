﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderHierarchyLib.Render.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using RenderHierarchyLib.Extensions.MonoGame;
using System.Diagnostics;

namespace RenderHierarchyLib.Models.Text
{
    public partial class CustomSpriteFont
    {
        public static class Errors
        {
            public const string TextContainsUnresolvableCharacters = "Text contains characters that cannot be resolved by this SpriteFont.";

            public const string UnresolvableCharacter = "Character cannot be resolved by this SpriteFont.";
        }
        public class CharComparer : IEqualityComparer<char>
        {
            public static readonly CharComparer Default = new CharComparer();

            public bool Equals(char x, char y)
            {
                return x == y;
            }

            public int GetHashCode(char b)
            {
                return b;
            }
        }

        public struct Glyph
        {
            public char Character;
            public Rectangle BoundsInTexture;
            public Rectangle Cropping;

            public float LeftBearing;
            public float RightBearing;
            public float Width;

            public float WidthIncludingBearings => LeftBearing + Width + RightBearing;

            public static readonly Glyph Empty;

            public Glyph(SpriteFont.Glyph glyph)
            {
                Character = glyph.Character;
                BoundsInTexture = glyph.BoundsInTexture;
                Cropping = glyph.Cropping;

                LeftBearing = Math.Max(glyph.LeftSideBearing, 0f);
                RightBearing = Math.Max(glyph.RightSideBearing, 0f);
                Width = glyph.Width;
            }

            public override readonly string ToString()
            {
                return string.Join(',',
                    $"{nameof(Character)}={Character}",
                    $"{nameof(BoundsInTexture)}={BoundsInTexture}",
                    $"{nameof(LeftBearing)}={LeftBearing}",
                    $"{nameof(RightBearing)}={RightBearing}",
                    $"{nameof(Width)}={Width}",
                    $"{nameof(Cropping)}={Cropping}");
            }
        }

        private readonly Glyph[] _glyphs;
        private readonly CharacterRegion[] _regions;
        private char? _defaultCharacter;
        private int _defaultGlyphIndex = -1;

        private readonly Texture2D _texture;
        public Vector2 TextureTexel { get; }

        public Glyph[] Glyphs => _glyphs;
        public CharacterRegion[] Regions => _regions;
        public Texture2D Texture => _texture;
        public ReadOnlyCollection<char> Characters { get; private set; }

        public float WidthSpacing { get; set; }
        public float HeightSpacing { get; set; }

        public char? DefaultCharacter
        {
            get
            {
                return _defaultCharacter;
            }
            set
            {
                if (value.HasValue)
                {
                    if (!TryGetGlyphIndex(value.Value, out _defaultGlyphIndex))
                    {
                        throw new ArgumentException("Character cannot be resolved by this SpriteFont.");
                    }

                }
                else
                {
                    _defaultGlyphIndex = -1;
                }

                _defaultCharacter = value;
            }
        }

        public SpriteFont DefaultFont { get; set; } = null;


        public CustomSpriteFont(SpriteFont font)
        {
            Characters = font.Characters;
            _texture = font.Texture;
            TextureTexel = new Vector2(1f / font.Texture.Width, 1f / font.Texture.Height);
            HeightSpacing = font.LineSpacing;
            WidthSpacing = font.Spacing;
            _glyphs = font.GetGlyphsArray();
            _regions = font.GetCharacterRegions();
            DefaultCharacter = font.DefaultCharacter;
            DefaultFont = font;
        }

        public CustomSpriteFont(Texture2D texture, List<Rectangle> glyphBounds, List<Rectangle> cropping, List<char> characters, int lineSpacing, float spacing, List<Vector3> kerning, char? defaultCharacter)
        {
            Characters = new ReadOnlyCollection<char>(characters.ToArray());
            _texture = texture;
            TextureTexel = new Vector2(1f / texture.Width, 1f / texture.Height);
            HeightSpacing = lineSpacing;
            WidthSpacing = spacing;
            _glyphs = new Glyph[characters.Count];
            var stack = new Stack<CharacterRegion>();
            for (int i = 0; i < characters.Count; i++)
            {
                _glyphs[i] = new Glyph
                {
                    Character = characters[i],
                    BoundsInTexture = glyphBounds[i],
                    LeftBearing = kerning[i].X,
                    RightBearing = kerning[i].Z,
                    Width = kerning[i].Y,
                    Cropping = cropping[i]
                };
                if (stack.Count == 0 || characters[i] > stack.Peek().End + 1)
                {
                    stack.Push(new CharacterRegion(characters[i], i));
                    continue;
                }
                if (characters[i] == stack.Peek().End + 1)
                {
                    CharacterRegion item = stack.Pop();
                    item.End += '\u0001';
                    stack.Push(item);
                    continue;
                }

                throw new InvalidOperationException("Invalid SpriteFont. Character map must be in ascending order.");
            }

            _regions = stack.ToArray();
            Array.Reverse(_regions);
            DefaultCharacter = defaultCharacter;
        }

        public Dictionary<char, Glyph> GetGlyphs()
        {
            Dictionary<char, Glyph> dictionary = new Dictionary<char, Glyph>(_glyphs.Length, CharComparer.Default);
            Glyph[] glyphs = _glyphs;
            for (int i = 0; i < glyphs.Length; i++)
            {
                Glyph value = glyphs[i];
                dictionary.Add(value.Character, value);
            }

            return dictionary;
        }

        public unsafe void SetGlyphIndexes(char* ptrText, int textLength, UnsafeList<int> glyphIndexes, out int lines)
        {
            glyphIndexes.Clear();
            glyphIndexes.EnsureCapacity(textLength);
            lines = 1;

            fixed (int* ptrGlyphIndex = glyphIndexes.Items)
            {
                fixed (CharacterRegion* ptrRegion = _regions)
                {
                    for (int i = 0; i < textLength; i++)
                    {
                        GetGlyphIndex(ref ptrText[i], ptrRegion, ref ptrGlyphIndex[i]);
                        if (ptrText[i] == '\n') lines++;
                    }
                }
            }

            glyphIndexes.Size = textLength;
        }

        public unsafe void MeasureString(string text, out Vector2 size)
        {
            if (text.Length == 0)
            {
                size = Vector2.Zero;
                return;
            }

            float num = 0f;
            float num2 = HeightSpacing;
            Vector2 zero = Vector2.Zero;
            bool flag = true;
            fixed (Glyph* ptr = Glyphs)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    switch (c)
                    {
                        case '\n':
                            num2 = HeightSpacing;
                            zero.X = 0f;
                            zero.Y += HeightSpacing;
                            flag = true;
                            continue;
                        case '\r':
                            continue;
                    }

                    int glyphIndexOrDefault = GetGlyphIndexOrDefault(c);
                    var ptr2 = ptr + glyphIndexOrDefault;
                    if (flag)
                    {
                        zero.X = Math.Max(ptr2->LeftBearing, 0f);
                        flag = false;
                    }
                    else
                    {
                        zero.X += WidthSpacing + ptr2->LeftBearing;
                    }

                    zero.X += ptr2->Width;
                    float num3 = zero.X + Math.Max(ptr2->RightBearing, 0f);
                    if (num3 > num)
                    {
                        num = num3;
                    }

                    zero.X += ptr2->RightBearing;
                    if (ptr2->Cropping.Height > num2)
                    {
                        num2 = ptr2->Cropping.Height;
                    }
                }
            }

            size.X = num;
            size.Y = zero.Y + num2;
        }

        public unsafe void MeasureStringPtr(int* ptrGlyphIndex, Glyph* ptrGlyph, char* ptrText, int length, out Vector2 size)
        {
            float sizeX = 0f;
            float num2 = HeightSpacing;
            Vector2 zero = Vector2.Zero;
            var flagLines = true;

            for (int i = 0; i < length; i++)
            {
                switch (ptrText[i])
                {
                    case '\n':
                        num2 = HeightSpacing;
                        zero.X = 0f;
                        zero.Y += HeightSpacing;
                        flagLines = true;
                        continue;
                    case '\r':
                        continue;
                }

                Glyph glyph = ptrGlyph[ptrGlyphIndex[i]];
                if (flagLines)
                {
                    zero.X = Math.Max(glyph.LeftBearing, 0f);
                    flagLines = false;
                }
                else
                {
                    zero.X += WidthSpacing + glyph.LeftBearing;
                }

                zero.X += glyph.Width;
                float num3 = zero.X + Math.Max(glyph.RightBearing, 0f);
                if (num3 > sizeX)
                {
                    sizeX = num3;
                }

                zero.X += glyph.RightBearing;
                if (glyph.Cropping.Height > num2)
                {
                    num2 = glyph.Cropping.Height;
                }
            }

            size.X = sizeX;
            size.Y = zero.Y + num2;
        }



        public int GetGlyphIndexOrDefault(char c)
        {
            if (!TryGetGlyphIndex(c, out var index))
                throw new ArgumentException("Text contains characters that cannot be resolved by this SpriteFont.", "text");
            return index;
        }
        public unsafe bool TryGetGlyphIndex(char c, out int index)
        {
            index = -1;
            fixed (CharacterRegion* ptrRegion = _regions)
                GetGlyphIndex(ref c, ptrRegion, ref index);
            return index != -1;
        }
        public unsafe void GetGlyphIndex(ref char c, CharacterRegion* ptrRegion, ref int index)
        {
            var num = -1;
            var num2 = 0;
            var num3 = _regions.Length - 1;
            while (num2 <= num3)
            {
                var num4 = num2 + num3 >> 1;
                if (ptrRegion[num4].End < c)
                {
                    num2 = num4 + 1;
                    continue;
                }

                if (ptrRegion[num4].Start > c)
                {
                    num3 = num4 - 1;
                    continue;
                }

                num = num4;
                break;
            }

            if (num == -1)
            {
                index = -1;
                return;
            }

            index = ptrRegion[num].StartIndex + (c - ptrRegion[num].Start);
        }
    }
}
