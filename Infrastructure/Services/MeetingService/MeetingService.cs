using System.Net;
using AutoMapper;
using Domain.DTOs.MeetingDto;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Infrastructure.Services.MeetingService;

public class MeetingService : IMeetingService
{
    private readonly IMapper _mapper;
    private readonly DataContext _context;
    private readonly ILogger<MeetingService> _logger;


    public MeetingService(IMapper mapper, DataContext context, ILogger<MeetingService> logger)
    {
        _mapper = mapper;
        _context = context;
        _logger = logger;

    }
    public async Task<Response<string>> AddMeetingAsync(CreateMeetingDto addMeetingDto)
    {
        try
        {
            var mapped = _mapper.Map<Meeting>(addMeetingDto);
            await _context.Meetings.AddAsync(mapped);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Success method Add in : {DateTime}", DateTime.UtcNow);
            return new Response<string>("Meeting added successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in method Add in time : {DateTime}", DateTime.UtcNow);
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }


    public async Task<PagedResponse<List<GetMeetingDto>>> GetMeetingAsync(MeetingFilter filter)
    {
        try
        {
            var Meetings = _context.Meetings.AsQueryable();

            if (!string.IsNullOrEmpty(filter.Name))
                Meetings = Meetings.Where(x => x.Name == filter.Name);

            if (!string.IsNullOrEmpty(filter.Description))
                Meetings = Meetings.Where(x => x.Description == filter.Description);

            var Meeting = await Meetings.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();
            var total = await Meetings.CountAsync();

            var response = _mapper.Map<List<GetMeetingDto>>(Meetings);
            _logger.LogInformation("Success method Get in : {DateTime}", DateTime.UtcNow);
            return new PagedResponse<List<GetMeetingDto>>(response, total, filter.PageNumber, filter.PageSize);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in method Get in time : {DateTime}", DateTime.UtcNow);
            return new PagedResponse<List<GetMeetingDto>>(HttpStatusCode.InternalServerError, e.Message);
        }
    }

    public async Task<Response<bool>> DeleteMeetingAsync(int id)
    {
        try
        {
            var existing = await _context.Meetings.Where(e => e.Id == id).ExecuteDeleteAsync();
            if (existing == 0) return new Response<bool>(HttpStatusCode.BadRequest, "Meeting not found!");
            _logger.LogInformation("Success method Delete in : {DateTime}", DateTime.UtcNow);
            return new Response<bool>(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in method Delete in time : {DateTime}", DateTime.UtcNow);
            return new Response<bool>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }


    public async Task<Response<string>> UpdateMeetingAsync(UpdateMeetingDto updateMeetingDto)
    {
        try
        {
            var existing = await _context.Meetings.AnyAsync(e => e.Id == updateMeetingDto.Id);
            if (!existing) return new Response<string>(HttpStatusCode.BadRequest, "Meeting not found!");
            var mapped = _mapper.Map<Meeting>(updateMeetingDto);
            _context.Meetings.Update(mapped);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Success method Update in : {DateTime}", DateTime.UtcNow);
            return new Response<string>("Updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in method Update in time : {DateTime}", DateTime.UtcNow);
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<GetMeetingDto>> GetMeetingByIdAsync(int id)
    {
        try
        {
            var existing = await _context.Meetings.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null)
            {
                return new Response<GetMeetingDto>(HttpStatusCode.BadRequest, "Meeting not found");
            }
            var Meeting = _mapper.Map<GetMeetingDto>(existing);
            _logger.LogInformation("Success method GetById in : {DateTime}", DateTime.UtcNow);
            return new Response<GetMeetingDto>(Meeting);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in method GetById in time : {DateTime}", DateTime.UtcNow);
            return new Response<GetMeetingDto>(HttpStatusCode.InternalServerError, e.Message);
        }
    }


    public async Task<PagedResponse<List<GetMeetingDto>>> GetUpcomingMeetingsAsync(MeetingFilter filter, int userId)
    {
        try
        {
            var meetings = _context.Meetings.AsQueryable();

            var response = await meetings.Select(e => new GetMeetingDto()
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                UserId = e.UserId
            })
            .Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize)
            .Where(e => e.UserId == userId && e.StartDate > DateTime.UtcNow).OrderByDescending(e => e.StartDate).ToListAsync();

            var total = await meetings.CountAsync();

            _logger.LogInformation("Success method GetUpcomingMeetings in : {DateTime}", DateTime.UtcNow);
            return new PagedResponse<List<GetMeetingDto>>(response, filter.PageNumber, filter.PageSize, total);
            }
        catch (Exception e)
        {
            _logger.LogError("Error in method GetUpcomingMeetings in time : {DateTime}", DateTime.UtcNow);
            return new PagedResponse<List<GetMeetingDto>>(HttpStatusCode.InternalServerError, e.Message);
        }
    }
}