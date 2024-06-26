﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSphere.Business.Services.Interfaces;
using EventSphere.Domain.DTOs;
using EventSphere.Domain.Entities;
using EventSphere.Infrastructure;
using EventSphere.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace EventSphere.Business.Services
{
    public class EventServiceBase
    {
        protected readonly EventSphereDbContext _context;

        public EventServiceBase(EventSphereDbContext context)
        {
            _context = context;
        }
    }

    public class EventService : EventServiceBase, IEventService
    {
        private readonly IGenericRepository<Event> _eventRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<EventCategory> _eventCategoryRepository;
        private readonly IGenericRepository<Location> _locationRepository;

        public EventService(EventSphereDbContext context,
            IGenericRepository<Event> eventRepository,
            IGenericRepository<User> userRepository,
            IGenericRepository<EventCategory> eventCategoryRepository,
            IGenericRepository<Location> locationRepository) : base(context)
        {
            _eventRepository = eventRepository;
            _userRepository = userRepository;
            _eventCategoryRepository = eventCategoryRepository;
            _locationRepository = locationRepository;
        }

        public async Task<IEnumerable<Event>> GetEventsByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return await _eventRepository.GetAllAsync();
            }

            return await _eventRepository.GetAsync(e => e.EventName.Contains(name));
        }

        public async Task<IEnumerable<Event>> GetAllEventsAsync()
        {
            return await _eventRepository.GetAllAsync();
        }

        public async Task<Event> GetEventsByIdAsync(int id)
        {
            return await _eventRepository.GetByIdAsync(id);
        }

        public async Task<Event> CreateEventsAsync(EventDTO eventDto, IFormFile image)
        {
            if (eventDto == null || image == null || image.Length == 0)
            {
                throw new ArgumentException("Event DTO or image is null or empty.");
            }

            try
            {
                string base64Image = await ResizeAndConvertToBase64Async(image);


                var user = await _userRepository.GetByIdAsync(eventDto.OrganizerId);
                var userName = user.Name;  
                var category = await _eventCategoryRepository.GetByIdAsync(eventDto.CategoryId);

                var categoryName = category.CategoryName;
                var location = await _locationRepository.GetByIdAsync(eventDto.LocationId);

                var events = new Event
                {
                    EventName = eventDto.EventName,
                    Description = eventDto.Description,
                    Address = eventDto.Address,
                    Location = location,
                    StartDate = eventDto.StartDate,
                    EndDate = eventDto.EndDate,
                    CategoryId = eventDto.CategoryId,
                    CategoryName = categoryName,
                    OrganizerId = eventDto.OrganizerId,


                    Category = category,
                    Organizer = user,

                    OrganizerName = userName,
                    PhotoData = base64Image,
                    MaxAttendance = eventDto.MaxAttendance,
                    AvailableTickets = eventDto.AvailableTickets,
                };

                await _eventRepository.AddAsync(events);
                return events;
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while creating the event.", ex);
            }
        }


        public static async Task<string> ResizeAndConvertToBase64Async(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                throw new ArgumentException("Image file is null or empty.");
            }

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await image.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    using (var img = Image.Load<Rgba32>(memoryStream))
                    {
                        img.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(500, 500),
                            Mode = ResizeMode.Max
                        }));

                        using (var outputMemoryStream = new MemoryStream())
                        {
                            img.Save(outputMemoryStream, new JpegEncoder { Quality = 100 });

                            byte[] imageBytes = outputMemoryStream.ToArray();
                            string base64String = Convert.ToBase64String(imageBytes);
                            return base64String;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while resizing and converting the image to Base64.", ex);
            }
        }

        public async Task<Event> UpdateEventsAsync(int id, EventDTO eventDto, IFormFile newImage)
        {
            var eventById = await _eventRepository.GetByIdAsync(id);
            if (eventById == null)
            {
                throw new ArgumentException($"Event with Id {id} not found.");
            }

            try
            {
                var eventPhotoData = eventById.PhotoData;

                eventById.EventName = eventDto.EventName;
                eventById.Description = eventDto.Description;
                eventById.LocationId = eventDto.LocationId;
                eventById.StartDate = eventDto.StartDate;
                eventById.EndDate = eventDto.EndDate;
                eventById.CategoryId = eventDto.CategoryId;
                eventById.OrganizerId = eventDto.OrganizerId;
                eventById.MaxAttendance = eventDto.MaxAttendance;
                eventById.AvailableTickets = eventDto.AvailableTickets;
                eventById.DateCreated = eventDto.DateCreated;

                if (newImage != null)
                {
                    string base64Image = await ResizeAndConvertToBase64Async(newImage);
                    eventById.PhotoData = base64Image;
                }
                else
                {
                    eventById.PhotoData = eventPhotoData;
                }

                await _eventRepository.UpdateAsync(eventById);

                return eventById;
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while updating the event.", ex);
            }
        }



        public async Task DeleteEventsAsync(int id)
        {
            await _eventRepository.DeleteAsync(id);
        }

        public async Task<int> GetEventCountAsync()
        {
            return await _eventRepository.CountAsync();
        }

        public async Task<IEnumerable<Event>> GetEventByCategoryId(int eventCategoryId)
        {
            return await _context.Events.Where(u => u.CategoryId == eventCategoryId).ToListAsync();
        }
        
        public async Task<IEnumerable<Event>> GetEventByOrganizerId(int organizerId)
        {
            return await _context.Events.Where(u => u.OrganizerId == organizerId).ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetEventsByCity(string city)
        {
            return await _context.Events.Include(e => e.Location).Where(e => e.Location.City == city).ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetEventsByCountry(string country)
        {
            return await _context.Events.Include(e => e.Location).Where(e => e.Location.Country == country).ToListAsync();
        }
    }
}
