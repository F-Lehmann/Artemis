﻿using System.Collections.Generic;
using System.Linq;
using Artemis.Storage.Entities.Surface;
using Artemis.Storage.Repositories.Interfaces;
using LiteDB;

namespace Artemis.Storage.Repositories
{
    internal class SurfaceRepository : ISurfaceRepository
    {
        private readonly LiteRepository _repository;

        public SurfaceRepository(LiteRepository repository)
        {
            _repository = repository;
            _repository.Database.GetCollection<SurfaceEntity>().EnsureIndex(s => s.Name);
        }

        public void Add(SurfaceEntity surfaceEntity)
        {
            _repository.Insert(surfaceEntity);
        }

        public void Remove(SurfaceEntity surfaceEntity)
        {
            _repository.Delete<SurfaceEntity>(surfaceEntity.Id);
        }

        public SurfaceEntity GetByName(string name)
        {
            return _repository.FirstOrDefault<SurfaceEntity>(s => s.Name == name);
        }

        public List<SurfaceEntity> GetAll()
        {
            return _repository.Query<SurfaceEntity>().Include(s => s.DeviceEntities.Select(de => de.InputIdentifiers)).ToList();
        }

        public void Save(SurfaceEntity surfaceEntity)
        {
            _repository.Upsert(surfaceEntity);
        }
    }
}