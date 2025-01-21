using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using _6.NovaPoshta.Data;
using _6.NovaPoshta.Data.Entities;
using _6.NovaPoshta.Models;
using _6.NovaPoshta.Models.Area;
using _6.NovaPoshta.Models.City;
using _6.NovaPoshta.Models.Department;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace _6.NovaPoshta
{
    public class NovaPoshtaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _url;
        private readonly BombaDbContext _context;

        public NovaPoshtaService()
        {
            _httpClient = new HttpClient();
            _url = "https://api.novaposhta.ua/v2.0/json/";
            _context = new BombaDbContext();
            _context.Database.Migrate(); //накатує на БД усі міграції, яких там немає
        }

        public async Task  SeedAreas()
        {
            if (!_context.Areas.Any()) // Якщо таблиця пуста
            {
                var modelRequest = new AreaPostModel
                {
                    ApiKey = "c44c00290a5023fcc0ff81091471dda1",
                    ModelName = "Address",
                    CalledMethod = "getAreas",
                    MethodProperties = new MethodProperties() { }
                };

                string json = JsonConvert.SerializeObject(modelRequest, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Formatting = Formatting.Indented // Для кращого вигляду (не обов'язково)
                });
                HttpContent context = new StringContent(json, Encoding.UTF8, "application/json");
                //тут забрав result
                HttpResponseMessage response = await _httpClient.PostAsync(_url, context);
                if (response.IsSuccessStatusCode)
                {

                    string jsonResp = await response.Content.ReadAsStringAsync(); //Читаємо відповідь від сервера
                    var result = JsonConvert.DeserializeObject<AreaResponse>(jsonResp);
                    if (result != null && result.Data != null && result.Success)
                    {
                        foreach (var item in result.Data)
                        {
                            var entity = new AreaEntity
                            {
                                Ref = item.Ref,
                                AreasCenter = item.AreasCenter,
                                Description = item.Description,
                            };
                            //Ці два методи відпрацьовують асинхронно
                      
                                await _context.Areas.AddAsync(entity);
                                await _context.SaveChangesAsync();                        
                        }
                    }
                }
            }
        }



        public async Task SeedCities()
        {
            if (!_context.Cities.Any()) // Якщо таблиця пуста
            {
                var listAreas = await GetListAreas(); 
                var allCities = new List<CityEntity>();

                foreach (var area in listAreas)
                {
                    if (area == null || string.IsNullOrEmpty(area.Ref))
                    {
                        // Якщо область не валідна, пропустити її
                        continue;
                    }

                    Console.WriteLine("Seed area {0}...", area.Description);

                    var modelRequest = new CityPostModel
                    {
                        ApiKey = "c44c00290a5023fcc0ff81091471dda1",
                        MethodProperties = new MethodCityProperties()
                        {
                            AreaRef = area.Ref
                        }
                    };

                    string json = JsonConvert.SerializeObject(modelRequest, new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented 
                    });

                    HttpContent context = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await _httpClient.PostAsync(_url, context);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResp = await response.Content.ReadAsStringAsync(); // Читаємо відповідь від сервера
                        var result = JsonConvert.DeserializeObject<CityResponse>(jsonResp);

                        if (result != null && result.Data != null && result.Success)
                        {
                            foreach (var city in result.Data)
                            {
                                if (city == null) continue; 

                                var cityEntity = new CityEntity
                                {
                                    Ref = city.Ref,
                                    Description = city.Description,
                                    TypeDescription = city.SettlementTypeDescription,
                                    AreaRef = city.Area,
                                    AreaId = area.Id
                                };

                                allCities.Add(cityEntity); 
                            }
                        }
                        else
                        {
                            Console.WriteLine("No cities found for area {0}", area.Description);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to get cities for area {0}", area.Description);
                    }
                }

                if (allCities.Any())
                {
                    await _context.Cities.AddRangeAsync(allCities); // Додаємо всі міста за один раз
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task SeedDepartments()
        {
            if (!_context.Departments.Any()) // Якщо таблиця пуста
            {
                var listCities = await _context.Cities.ToListAsync(); 
                var allDepartments = new ConcurrentBag<DepartmentEntity>(); 
                var tasks = new List<Task>();
                var semaphore = new SemaphoreSlim(10); // Ліміт одночасних задач (10)

                foreach (var city in listCities)
                {
                    await semaphore.WaitAsync(); // Чекаємо, якщо ліміт досягнуто
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            Console.WriteLine("Seed city {0}...", city.Description);

                            var modelRequest = new DepartmentPostModel
                            {
                                ApiKey = "c44c00290a5023fcc0ff81091471dda1",
                                MethodProperties = new MethodDepartmentProperties
                                {
                                    CityRef = city.Ref
                                }
                            };

                            string json = JsonConvert.SerializeObject(modelRequest, new JsonSerializerSettings
                            {
                                Formatting = Formatting.Indented
                            });

                            HttpContent context = new StringContent(json, Encoding.UTF8, "application/json");
                            HttpResponseMessage response = await _httpClient.PostAsync(_url, context);

                            if (response.IsSuccessStatusCode)
                            {
                                string jsonResp = await response.Content.ReadAsStringAsync(); // Читаємо відповідь від сервера
                                var result = JsonConvert.DeserializeObject<DepartmentResponse>(jsonResp);

                                if (result != null && result.Data != null && result.Success)
                                {
                                    foreach (var dep in result.Data)
                                    {
                                        var departmentEntity = new DepartmentEntity
                                        {
                                            Ref = dep.Ref,
                                            Description = dep.Description,
                                            Address = dep.ShortAddress,
                                            Phone = dep.Phone,
                                            CityRef = dep.CityRef,
                                            CityId = city.Id
                                        };
                                        allDepartments.Add(departmentEntity); // додавання
                                    }
                                }
                            }
                        }
                        finally
                        {
                            semaphore.Release(); // Звільняю семафор
                        }
                    }));
                }

                await Task.WhenAll(tasks); // Чекаємо завершення всіх задач

                if (allDepartments.Any())
                {
                    await _context.Departments.AddRangeAsync(allDepartments); // Масове додавання в базу даних
                    await _context.SaveChangesAsync(); 
                }
            }
        }


        public async Task<List<AreaEntity>> GetListAreas()
        {
            return await _context.Areas.ToListAsync();
        }


    }
}