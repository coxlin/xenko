﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Runtime.CompilerServices;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Serialization.Serializers;
using SiliconStudio.Core.Storage;

namespace SiliconStudio.Core.Serialization
{
    public static class UrlServices
    {
        private static ConditionalWeakTable<object, UrlInfo> urls = new ConditionalWeakTable<object, UrlInfo>();

        public static string GetUrl(object obj)
        {
            UrlInfo urlInfo;
            return urls.TryGetValue(obj, out urlInfo) ? urlInfo.Url : null;
        }

        public static void SetUrl(object obj, string url)
        {
            var urlInfo = urls.GetValue(obj, x => new UrlInfo());
            urlInfo.Url = url;
        }

        public static UrlInfo GetUrlInfo(object obj)
        {
            UrlInfo urlInfo;
            urls.TryGetValue(obj, out urlInfo);
            return urlInfo;
        }

        public static UrlInfo GetOrCreateUrlInfo(object obj)
        {
            return urls.GetValue(obj, x => new UrlInfo());
        }

        public static T CreateSerializableVersion<T>(Guid id, string location) where T : class, new()
        {
            var result = new T();
            var urlInfo = GetOrCreateUrlInfo(result);
            urlInfo.Id = id;
            urlInfo.Url = location;
            urlInfo.IsProxy = true;
            return result;
        }

        public static object CreateSerializableVersion(Type type, Guid id, string location)
        {
            var result = Activator.CreateInstance(type);
            var urlInfo = GetOrCreateUrlInfo(result);
            urlInfo.Id = id;
            urlInfo.Url = location;
            urlInfo.IsProxy = true;
            return result;
        }

        public class UrlInfo
        {
            public string Url;

            public Guid Id;

            /// <summary>
            /// If yes, it won't be recursively saved.
            /// Use this if you only care about the Url.
            /// </summary>
            public bool IsProxy;

            /// <summary>
            /// Data representation.
            /// </summary>
            public object Data;
        }
    }

    public abstract class ContentReference : ITypedContentReference, IEquatable<ContentReference>
    {
        internal const int NullIdentifier = -1;

        /// <summary>
        /// Creates a content reference with the specified value..
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>ContentReference{``0}.</returns>
        public static ContentReference<T> Create<T>(T value) where T : class
        {
            return new ContentReference<T>() { Value = value };
        }

        /// <summary>
        /// Gets or sets the asset unique identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the URL of the referenced data.
        /// </summary>
        /// <value>
        /// The URL of the referenced data.
        /// </value>
        public abstract string Location { get; set; }

        public ContentReferenceState State { get; set; }

        public abstract Type Type { get; }

        public abstract object ObjectValue { get; set; }


        public bool Equals(ContentReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id) && Equals(Location, other.Location);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ContentReference)obj);
        }

        /// <summary>
        /// Creates a new reference of the specified type with given id and location.
        /// </summary>
        /// <param name="referenceType">Type of the reference.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="location">The location.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">referenceType</exception>
        /// <exception cref="System.ArgumentException">Reference must inherit from ContentReference;referenceType</exception>
        public static ContentReference New(Type referenceType, Guid id, string location)
        {
            if (referenceType == null) throw new ArgumentNullException("referenceType");
            if (!typeof(ContentReference).IsAssignableFrom(referenceType)) throw new ArgumentException("Reference must inherit from ContentReference", "referenceType");

            return (ContentReference)Activator.CreateInstance(referenceType, id, location);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                return (Id.GetHashCode()*397) ^ (Location != null ? Location.GetHashCode() : 0);
            }
        }

        public static bool operator ==(ContentReference left, ContentReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ContentReference left, ContentReference right)
        {
            return !Equals(left, right);
        }


        public override string ToString()
        {
            return string.Format("{0}:{1}", Id, Location);
        }
    }

    [DataSerializer(typeof(ContentReferenceDataSerializer<>), Mode = DataSerializerGenericMode.GenericArguments)]
    public sealed class ContentReference<T> : ContentReference where T : class
    {
        // Depending on state, either value or Location is null (they can't be both non-null)
        private T value;
        private string url;

        public override Type Type
        {
            get { return typeof(T); }
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        /// <exception cref="System.InvalidOperationException">Value can't be read in this state.</exception>
        public T Value
        {
            get
            {
                //if (State == ContentReferenceState.NeverLoad)
                //    throw new InvalidOperationException("Value can't be read in this state.");
                //lock (assetManager)
                //{
                //    if (State == ContentReferenceState.LoadFirstAccess)
                //    {
                //        value = assetManager.Load<T>(Location);
                //        State = ContentReferenceState.Loaded;
                //    }
                //    else if (State == ContentReferenceState.LoadEverytime)
                //    {
                //        return assetManager.Load<T>(Location);
                //    }
                //}
                return value;
            }
            set
            {
                State = ContentReferenceState.Modified;
                this.value = value;
                this.url = null;
            }
        }

        public override string Location
        {
            get
            {
                // TODO: Should we return value.Location if value is not null, or just reference Location?
                if (value == null)
                    return url;

                return UrlServices.GetUrl(value);
            }
            set
            {
                if (this.value == null)
                {
                    url = value;
                }
                else
                {
                    UrlServices.SetUrl(this.value, value);
                }
            }
        }

        public override object ObjectValue
        {
            get { return Value; }
            set { Value = (T)value; }
        }

        public ContentReference()
        {
            Value = null;
        }

        public ContentReference(Guid id, string location)
        {
            Id = id;
            Location = location;
        }

        public static explicit operator T (ContentReference<T> contentReference)
        {
            if (contentReference == null)
                return null;
            return contentReference.Value;
        }

        public static implicit operator ContentReference<T>(T value)
        {
            return new ContentReference<T>() { Value = value };
        }


    }
}