﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Linq;
using SiliconStudio.Assets.Visitors;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Paradox.EntityModel;
using SiliconStudio.Paradox.EntityModel.Data;

namespace SiliconStudio.Paradox.Assets.Model.Analysis
{
    public static class EntityAnalysis
    {
        public struct Result
        {
            public List<EntityLink> EntityReferences;
        }

        public static Result Visit(EntityHierarchyData entityHierarchy)
        {
            if (entityHierarchy == null) throw new ArgumentNullException("obj");

            var entityReferenceVistor = new EntityReferenceAnalysis();
            entityReferenceVistor.Visit(entityHierarchy);

            return entityReferenceVistor.Result;
        }

        /// <summary>
        /// Updates <see cref="EntityReference.Id"/>, <see cref="EntityReference.Name"/>, <see cref="EntityComponentReference{T}.Entity"/>
        /// and <see cref="EntityComponentReference{T}.Component"/>, while also checking integrity of given <see cref="EntityAsset"/>.
        /// </summary>
        /// <param name="entityHierarchy">The entity asset.</param>
        public static void UpdateEntityReferences(EntityHierarchyData entityHierarchy)
        {
        }

        /// <summary>
        /// Fixups the entity references, by clearing invalid <see cref="EntityReference.Id"/>, and updating <see cref="EntityReference.Value"/> (same for components).
        /// </summary>
        /// <param name="entityHierarchy">The entity asset.</param>
        public static void FixupEntityReferences(EntityHierarchyData entityHierarchy)
        {
            var entityAnalysisResult = Visit(entityHierarchy);

            // Reverse the list, so that we can still properly update everything
            // (i.e. if we have a[0], a[1], a[1].Test, we have to do it from back to front to be valid at each step)
            entityAnalysisResult.EntityReferences.Reverse();

            // Updates Entity/EntityComponent references
            foreach (var entityLink in entityAnalysisResult.EntityReferences)
            {
                object obj = null;

                if (entityLink.EntityComponent != null)
                {
                    var containingEntity = entityLink.EntityComponent.Entity;
                    if (containingEntity == null)
                        throw new InvalidOperationException("Found a reference to a component which doesn't have any entity");

                    Entity realEntity;
                    if (entityHierarchy.Entities.TryGetValue(containingEntity.Id, out realEntity)
                        && realEntity.Components.TryGetValue(entityLink.EntityComponent.DefaultKey, out obj))
                    {
                        // If we already have the proper item, let's skip
                        if (obj == entityLink.EntityComponent)
                            continue;
                    }
                }
                else
                {
                    Entity realEntity;
                    if (entityHierarchy.Entities.TryGetValue(entityLink.Entity.Id, out realEntity))
                    {
                        obj = realEntity;

                        // If we already have the proper item, let's skip
                        if (obj == entityLink.Entity)
                            continue;
                    }
                }

                if (obj != null)
                {
                    // We could find the referenced item, let's use it
                    entityLink.Path.Apply(entityHierarchy, MemberPathAction.ValueSet, obj);
                }
                else
                {
                    // Item could not be found, let's null it
                    entityLink.Path.Apply(entityHierarchy, MemberPathAction.ValueClear, null);
                }
            }
        }

        /// <summary>
        /// Remaps the entities identifier.
        /// </summary>
        /// <param name="entityHierarchy">The entity hierarchy.</param>
        /// <param name="idRemapping">The identifier remapping.</param>
        public static void RemapEntitiesId(EntityHierarchyData entityHierarchy, Dictionary<Guid, Guid> idRemapping)
        {
            Guid newId;

            // Remap entities in asset2 with new Id
            if (idRemapping.TryGetValue(entityHierarchy.RootEntity, out newId))
                entityHierarchy.RootEntity = newId;

            foreach (var entity in entityHierarchy.Entities)
            {
                if (idRemapping.TryGetValue(entity.Id, out newId))
                    entity.Id = newId;
            }

            // Sort again the EntityCollection (since ID changed)
            entityHierarchy.Entities.Sort();

            // Remap entity references with new Id
            var entityAnalysisResult = EntityAnalysis.Visit(entityHierarchy);
            foreach (var entityLink in entityAnalysisResult.EntityReferences)
            {
                if (idRemapping.TryGetValue(entityLink.Entity.Id, out newId))
                    entityLink.Entity.Id = newId;
            }
        }

        private class EntityReferenceAnalysis : AssetVisitorBase
        {
            private int componentDepth;

            public EntityReferenceAnalysis()
            {
                var result = new Result();
                result.EntityReferences = new List<EntityLink>();
                Result = result;
            }

            public Result Result { get; private set; }

            public override void VisitObject(object obj, ObjectDescriptor descriptor, bool visitMembers)
            {
                if (obj is EntityComponent)
                    componentDepth++;

                bool processObject = true;

                if (componentDepth >= 2)
                {
                    var entity = obj as Entity;
                    if (entity != null)
                    {
                        Result.EntityReferences.Add(new EntityLink(entity, CurrentPath.Clone()));
                        processObject = false;
                    }

                    var entityComponent = obj as EntityComponent;
                    if (entityComponent != null)
                    {
                        Result.EntityReferences.Add(new EntityLink(entityComponent, CurrentPath.Clone()));
                        processObject = false;
                    }
                }

                if (processObject)
                    base.VisitObject(obj, descriptor, visitMembers);

                if (obj is EntityComponent)
                    componentDepth--;
            }
        }

        public struct EntityLink
        {
            public readonly Entity Entity;
            public readonly EntityComponent EntityComponent;
            public readonly MemberPath Path;

            public EntityLink(Entity entity, MemberPath path)
            {
                Entity = entity;
                EntityComponent = null;
                Path = path;
            }

            public EntityLink(EntityComponent entityComponent, MemberPath path)
            {
                Entity = null;
                EntityComponent = entityComponent;
                Path = path;
            }
        }
    }
}