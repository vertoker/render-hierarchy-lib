﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderHierarchyLib.Core
{
    public class HierarchySpriteBatchItem : IComparable<HierarchySpriteBatchItem>
    {
        public Texture2D Texture;

        public float SortKey;

        public VertexPositionColorTexture vertexTL;

        public VertexPositionColorTexture vertexTR;

        public VertexPositionColorTexture vertexBL;

        public VertexPositionColorTexture vertexBR;

        public HierarchySpriteBatchItem()
        {
            vertexTL = default(VertexPositionColorTexture);
            vertexTR = default(VertexPositionColorTexture);
            vertexBL = default(VertexPositionColorTexture);
            vertexBR = default(VertexPositionColorTexture);
        }

        public void Set(float x, float y, float dx, float dy, float w, float h, float sin, float cos, Color color, Vector2 texCoordTL, Vector2 texCoordBR, float depth)
        {
            vertexTL.Position.X = x + dx * cos - dy * sin;
            vertexTL.Position.Y = y + dx * sin + dy * cos;
            vertexTL.Position.Z = depth;
            vertexTL.Color = color;
            vertexTL.TextureCoordinate.X = texCoordTL.X;
            vertexTL.TextureCoordinate.Y = texCoordTL.Y;
            vertexTR.Position.X = x + (dx + w) * cos - dy * sin;
            vertexTR.Position.Y = y + (dx + w) * sin + dy * cos;
            vertexTR.Position.Z = depth;
            vertexTR.Color = color;
            vertexTR.TextureCoordinate.X = texCoordBR.X;
            vertexTR.TextureCoordinate.Y = texCoordTL.Y;
            vertexBL.Position.X = x + dx * cos - (dy + h) * sin;
            vertexBL.Position.Y = y + dx * sin + (dy + h) * cos;
            vertexBL.Position.Z = depth;
            vertexBL.Color = color;
            vertexBL.TextureCoordinate.X = texCoordTL.X;
            vertexBL.TextureCoordinate.Y = texCoordBR.Y;
            vertexBR.Position.X = x + (dx + w) * cos - (dy + h) * sin;
            vertexBR.Position.Y = y + (dx + w) * sin + (dy + h) * cos;
            vertexBR.Position.Z = depth;
            vertexBR.Color = color;
            vertexBR.TextureCoordinate.X = texCoordBR.X;
            vertexBR.TextureCoordinate.Y = texCoordBR.Y;
        }

        public void Set(float x, float y, float w, float h, Color color, Vector2 texCoordTL, Vector2 texCoordBR, float depth)
        {
            vertexTL.Position.X = x;
            vertexTL.Position.Y = y;
            vertexTL.Position.Z = depth;
            vertexTL.Color = color;
            vertexTL.TextureCoordinate.X = texCoordTL.X;
            vertexTL.TextureCoordinate.Y = texCoordTL.Y;
            vertexTR.Position.X = x + w;
            vertexTR.Position.Y = y;
            vertexTR.Position.Z = depth;
            vertexTR.Color = color;
            vertexTR.TextureCoordinate.X = texCoordBR.X;
            vertexTR.TextureCoordinate.Y = texCoordTL.Y;
            vertexBL.Position.X = x;
            vertexBL.Position.Y = y + h;
            vertexBL.Position.Z = depth;
            vertexBL.Color = color;
            vertexBL.TextureCoordinate.X = texCoordTL.X;
            vertexBL.TextureCoordinate.Y = texCoordBR.Y;
            vertexBR.Position.X = x + w;
            vertexBR.Position.Y = y + h;
            vertexBR.Position.Z = depth;
            vertexBR.Color = color;
            vertexBR.TextureCoordinate.X = texCoordBR.X;
            vertexBR.TextureCoordinate.Y = texCoordBR.Y;
        }

        public int CompareTo(HierarchySpriteBatchItem other)
        {
            return SortKey.CompareTo(other.SortKey);
        }
    }
}