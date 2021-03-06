﻿using System;
using System.Collections.Generic;
using Ford.Domain;
using Ford.Infrastructure.Data.CarRepository.Interfaces;
using Ford.Infrastructure.Data.Context.Interfaces;
using Ford.Infrastructure.Data.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ford.Infrastructure.Data.CarRepository
{
    public class CarsRepository : ICarsRepository
    {
        private readonly ICarsMapper _carsMapper;
        private readonly IFordContext _fordContext;

        public CarsRepository(IFordContext fordContext, ICarsMapper carsMapper)
        {
            _fordContext = fordContext;
            _carsMapper = carsMapper;
        }

        public void Create(Car car)
        {
            var carSchema = _carsMapper.MapDomainToSchema(car);
            _fordContext.GetContext().GetCollection<CarSchema>("Cars").InsertOne(carSchema);
        }

        public IList<Car> GetAll()
        {
            var carSchema = _fordContext.GetContext().GetCollection<CarSchema>("Cars")
                .Find(x => x.Active).ToList();
            return _carsMapper.MapSchemaToDomain(carSchema);
        }

        public IList<Car> GetCollectionBy(string dbField, string valueCondition)
        {
            var filterByField = Builders<CarSchema>.Filter.Eq(dbField, valueCondition);
            var filterActiveRecords = Builders<CarSchema>.Filter.Eq(x => x.Active, true);
            var filter = Builders<CarSchema>.Filter.And(filterByField, filterActiveRecords);

            var carSChema = _fordContext.GetContext().GetCollection<CarSchema>("Cars")
                .Find(filter, new FindOptions
                {
                    Collation = new Collation("en", strength: CollationStrength.Secondary)
                }).ToList();

            return _carsMapper.MapSchemaToDomain(carSChema);
        }

        public void Delete(string id)
        {
            try
            {
                var filter = Builders<CarSchema>.Filter.Eq("_id", ObjectId.Parse(id));
                var update = Builders<CarSchema>.Update
                    .Set(s => s.Active, false)
                    .CurrentDate(s => s.UpdatedAt);

                _fordContext.GetContext().GetCollection<CarSchema>("Cars")
                    .UpdateOne(filter, update);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Car GetById(string id)
        {
            try
            {
                var filter = Builders<CarSchema>.Filter.Eq("_id", ObjectId.Parse(id));

                var carSChema = _fordContext.GetContext().GetCollection<CarSchema>("Cars")
                    .Find(filter).FirstOrDefault();

                return carSChema != null ? _carsMapper.MapSchemaToDomain(carSChema) : null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Update(Car car)
        {
            var carSchema = _carsMapper.MapDomainToSchema(car);
            carSchema.UpdatedAt = DateTime.Now;

            _fordContext.GetContext().GetCollection<CarSchema>("Cars")
                .FindOneAndReplace(x => x.Id == ObjectId.Parse(car.Id), carSchema);
        }
    }
}