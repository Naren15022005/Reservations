using FODUN.Reservations.Domain.Aggregates.Accommodation;
using FODUN.Reservations.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FODUN.Reservations.Infrastructure.Persistence;

/// <summary>
/// Pobla la base de datos InMemory con datos de prueba realistas.
/// Solo se ejecuta si la BD está vacía.
/// </summary>
public static class DbSeeder
{
    private static readonly DateOnly Since = new(2024, 1, 1);

    public static async Task SeedAsync(ReservationsDbContext context, ILogger logger)
    {
        if (await context.Accommodations.AnyAsync())
            return;

        logger.LogInformation("Sembrando datos de prueba en BD InMemory...");

        var accommodations = BuildAccommodations();
        await context.Accommodations.AddRangeAsync(accommodations);

        // Agregar tarifas por cada alojamiento
        var tariffs = BuildTariffs(accommodations);
        await context.Tariffs.AddRangeAsync(tariffs);

        await context.SaveChangesAsync();
        logger.LogInformation("Datos de prueba sembrados: {Count} sedes.", accommodations.Count);
    }

    private static List<Accommodation> BuildAccommodations()
    {
        // Sede 1 — Villeta (Recreacional)
        var villeta = Accommodation.Create(
            "SVL", "Sede Villeta", AccommodationType.RecreationalSite,
            "Villeta", 30,
            "Sede recreacional con piscina, zonas verdes y vista al río. Perfecta para grupos familiares.",
            "Km 5 Vía Villeta-Honda");
        var a01 = villeta.AddSeat("A01", "Cabaña Doble", 4, "Cabaña con 2 habitaciones, baño privado, sala y cocina.");
        a01.SetAmenities("Baño privado,Cocina equipada,Sala de estar,Aire acondicionado,Zona BBQ");
        var a02 = villeta.AddSeat("A02", "Cabaña Familiar", 8, "Cabaña amplia con 4 habitaciones, 2 baños y zona de BBQ.");
        a02.SetAmenities("2 baños,Cocina equipada,Zona BBQ,Hamacas,Jardín privado");
        var a03 = villeta.AddSeat("A03", "Dormitorio Compartido", 12, "Espacio tipo hostal con literas, ideal para grupos grandes.");
        a03.SetAmenities("Literas,Baño compartido,Casilleros,Ventilador,Zona social");

        // Sede 2 — Fusagasugá (Recreacional)
        var fusa = Accommodation.Create(
            "SFS", "Sede Fusagasugá", AccommodationType.RecreationalSite,
            "Fusagasugá", 20,
            "Sede en clima templado con jardines y canchas deportivas.",
            "Carrera 10 #15-30, Fusagasugá");
        var b01 = fusa.AddSeat("B01", "Habitación Estándar", 2, "Habitación con cama doble, baño privado y ventilador.");
        b01.SetAmenities("Baño privado,Cama doble,Ventilador,Televisor,Nevera");
        var b02 = fusa.AddSeat("B02", "Habitación Cuádruple", 4, "Habitación con 2 camas dobles y baño compartido.");
        b02.SetAmenities("2 camas dobles,Baño compartido,Televisor,Ventilador");
        var b03 = fusa.AddSeat("B03", "Suite Familiar", 6, "Suite con sala, 3 habitaciones y 2 baños privados.");
        b03.SetAmenities("3 habitaciones,2 baños,Sala de estar,Televisor,Nevera,Aire acondicionado");

        // Sede 3 — Apartamentos Medellín
        var medellin = Accommodation.Create(
            "APM", "Apartamentos Medellín", AccommodationType.Apartment,
            "Medellín", 12,
            "Apartamentos completamente equipados en el Poblado. Incluyen cocina, sala y wi-fi.",
            "Calle 10 #43-22, El Poblado, Medellín");
        var c01 = medellin.AddSeat("C01", "Apartamento Estudio", 2, "Estudio con cocina integrada y balcón.");
        c01.SetAmenities("Cocina integrada,Balcón,Wi-Fi,Televisor,Aire acondicionado");
        var c02 = medellin.AddSeat("C02", "Apartamento 1 Hab.", 4, "Apartamento con habitación separada y sala-comedor.");
        c02.SetAmenities("1 habitación,Sala-comedor,Cocina,Wi-Fi,Televisor,Aire acondicionado");
        var c03 = medellin.AddSeat("C03", "Apartamento 2 Hab.", 6, "Apartamento familiar con 2 habitaciones y 2 baños.");
        c03.SetAmenities("2 habitaciones,2 baños,Cocina equipada,Wi-Fi,Televisor,Lavadora");

        // Sede 4 — Santa Marta
        var santaMarta = Accommodation.Create(
            "APB", "Apartamentos Santa Marta", AccommodationType.Apartment,
            "Santa Marta", 8,
            "Apartamentos frente al mar en el Rodadero. Acceso directo a la playa.",
            "Calle 1 #2-10, El Rodadero, Santa Marta");
        var d01 = santaMarta.AddSeat("D01", "Apartamento Playa", 3, "Apartamento con vista al mar y terraza privada.");
        d01.SetAmenities("Vista al mar,Terraza privada,Aire acondicionado,Wi-Fi,Cocina");
        var d02 = santaMarta.AddSeat("D02", "Penthouse", 8, "Ático con 3 habitaciones, jacuzzi y vista panorámica.");
        d02.SetAmenities("3 habitaciones,Jacuzzi,Vista panorámica,2 baños,Cocina equipada,Wi-Fi");

        return [villeta, fusa, medellin, santaMarta];
    }

    private static List<Tariff> BuildTariffs(List<Accommodation> accommodations)
    {
        var tariffs = new List<Tariff>();

        foreach (var acc in accommodations)
        {
            foreach (var seat in acc.Seats)
            {
                // Precio base según capacidad
                decimal basePrice = seat.Capacity switch
                {
                    <= 2  => 120_000m,
                    <= 4  => 200_000m,
                    <= 6  => 280_000m,
                    <= 8  => 380_000m,
                    _     => 500_000m
                };

                // Tarifa baja (toda la semana)
                tariffs.Add(Tariff.Create(
                    seat.Id, SeasonType.Low, seat.Capacity * 2,
                    basePrice, Since,
                    additionalPersonPrice: 16_000m));

                // Tarifa alta (+40%, típicamente temporada vacacional)
                tariffs.Add(Tariff.Create(
                    seat.Id, SeasonType.High, seat.Capacity * 2,
                    Math.Round(basePrice * 1.4m / 1000m) * 1000m,
                    Since,
                    additionalPersonPrice: 18_000m));

                // Tarifa especial lun-jue: precio intermedio (+20%)
                tariffs.Add(Tariff.Create(
                    seat.Id, SeasonType.Special, seat.Capacity * 2,
                    Math.Round(basePrice * 1.2m / 1000m) * 1000m,
                    Since,
                    isExceptional: true,
                    daysOfWeek: "Mon,Tue,Wed,Thu",
                    additionalPersonPrice: 16_000m));
            }
        }

        return tariffs;
    }
}
