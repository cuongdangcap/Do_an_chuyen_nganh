using Admissions.Application.Auth;
using Admissions.Domain.Constants;
using Admissions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Admissions.Infrastructure.Persistence;

public sealed class DatabaseSeeder(
    AdmissionsDbContext dbContext,
    IPasswordHasher passwordHasher,
    IConfiguration configuration)
{
    private static readonly string[] CmcFacultyCodes = ["CMC-ICT", "CMC-ENG", "CMC-BUS", "CMC-MEDIA", "CMC-ART", "CMC-LANG"];

    private static readonly CmcProgramSeed[] CmcPrograms =
    [
        new("IT", "Công nghệ Thông tin", "CMC-ICT", "Máy tính và Công nghệ thông tin", 240, 140, "TECH", 14_742_000m, 18_018_000m, 21_840_000m),
        new("CS", "Khoa học Máy tính", "CMC-ICT", "Máy tính và Công nghệ thông tin", 80, 80, "TECH", 14_742_000m, 18_018_000m, 21_840_000m),
        new("AI", "Trí tuệ Nhân tạo", "CMC-ICT", "Máy tính và Công nghệ thông tin", 80, 100, "TECH", 14_742_000m, 18_018_000m, 21_840_000m),
        new("SE", "Kỹ thuật Phần mềm", "CMC-ICT", "Máy tính và Công nghệ thông tin", 80, null, "TECH", 14_742_000m, 18_018_000m, 21_840_000m),
        new("NS", "An ninh Mạng", "CMC-ICT", "Máy tính và Công nghệ thông tin", 40, null, "TECH", 14_742_000m, 18_018_000m, 21_840_000m),
        new("EC", "Công nghệ Kỹ thuật Điện tử - Viễn thông", "CMC-ENG", "Công nghệ kỹ thuật", 80, 40, "EC", 14_742_000m, 18_018_000m, 21_840_000m, Subtitle: "Thiết kế vi mạch bán dẫn"),
        new("BA", "Quản trị Kinh doanh", "CMC-BUS", "Kinh doanh và Quản lý", 160, 120, "BUS", 13_608_000m, 16_632_000m, 20_160_000m),
        new("LS", "Logistics và Quản lý chuỗi cung ứng", "CMC-BUS", "Kinh doanh và Quản lý", 80, 120, "BUS", 13_608_000m, 16_632_000m, 20_160_000m),
        new("MK", "Digital Marketing", "CMC-BUS", "Kinh doanh và Quản lý", 160, 120, "BUS", 13_608_000m, 16_632_000m, 20_160_000m),
        new("EM", "Thương mại Điện tử", "CMC-BUS", "Kinh doanh và Quản lý", 80, null, "BUS", 13_608_000m, 16_632_000m, 20_160_000m),
        new("IB", "Kinh doanh Quốc tế", "CMC-BUS", "Kinh doanh và Quản lý", 40, null, "BUS", 13_608_000m, 16_632_000m, 20_160_000m),
        new("MC", "Truyền thông Đa phương tiện", "CMC-MEDIA", "Báo chí và Truyền thông", 80, null, "BUS", 13_608_000m, 16_632_000m, 20_160_000m),
        new("PR", "Quan hệ Công chúng", "CMC-MEDIA", "Báo chí và Truyền thông", 40, null, "BUS", 13_608_000m, 16_632_000m, 20_160_000m),
        new("GD", "Thiết kế Đồ họa", "CMC-ART", "Nghệ thuật", 160, 80, "BUS", 13_608_000m, 16_632_000m, 20_160_000m),
        new("GA", "Đồ họa Game", "CMC-ART", "Nghệ thuật", 80, null, "BUS", 13_608_000m, 16_632_000m, 20_160_000m),
        new("DA", "Thiết kế Mỹ thuật số", "CMC-ART", "Nghệ thuật", 40, null, "BUS", 13_608_000m, 16_632_000m, 20_160_000m),
        new("KL", "Ngôn ngữ Hàn Quốc", "CMC-LANG", "Nhân văn", 80, null, "BUS", 12_474_000m, 15_246_000m, 18_480_000m),
        new("CL", "Ngôn ngữ Trung Quốc", "CMC-LANG", "Nhân văn", 160, null, "BUS", 12_474_000m, 15_246_000m, 18_480_000m),
        new("CB", "Tiếng Trung Thương mại", "CMC-LANG", "Nhân văn", 40, null, "BUS", 12_474_000m, 15_246_000m, 18_480_000m),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(cancellationToken);
        await SeedAdminAsync(cancellationToken);
        await SeedStudentAccountsAsync(cancellationToken);
        await SeedCmcAdmissionsAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await dbContext.Roles.Select(x => x.Code).ToListAsync(cancellationToken);
        foreach (var roleCode in RoleCodes.All.Except(existingCodes))
        {
            dbContext.Roles.Add(new Role
            {
                Code = roleCode,
                Name = char.ToUpperInvariant(roleCode[0]) + roleCode[1..],
                Description = $"Built-in {roleCode} role",
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        var email = configuration["SeedAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["SeedAdmin:Password"];
        var fullName = configuration["SeedAdmin:FullName"] ?? "System Admin";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            return;
        }

        var adminRole = await dbContext.Roles.FirstAsync(x => x.Code == RoleCodes.Admin, cancellationToken);
        var user = new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            FullName = fullName,
        };

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = adminRole.Id,
            Role = adminRole,
        });

        user.StaffProfile = new StaffProfile
        {
            UserId = user.Id,
            Department = "Phòng Tuyển sinh CMCU",
            Position = "Quản trị viên tuyển sinh",
            CanManageDocuments = true,
            CanReplyChat = true,
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedStudentAccountsAsync(CancellationToken cancellationToken)
    {
        var defaultPassword = configuration["SeedStudent:Password"] ?? configuration["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(defaultPassword))
        {
            return;
        }

        var studentRole = await dbContext.Roles.FirstAsync(x => x.Code == RoleCodes.Student, cancellationToken);

        for (var number = 240001; number <= 240100; number++)
        {
            var email = $"bit{number:000000}@st.cmcu.edu.vn";
            var index = number - 240001;
            var fullName = BuildStudentFullName(index);
            var existing = await dbContext.Users
                .Include(x => x.UserRoles)
                .Include(x => x.StudentProfile)
                .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
            if (existing is not null)
            {
                existing.FullName = fullName;
                existing.Phone ??= $"09{number % 100000000:00000000}";
                existing.EmailVerifiedAt ??= DateTime.UtcNow;
                existing.UpdatedAt = DateTime.UtcNow;
                if (!existing.UserRoles.Any(x => x.RoleId == studentRole.Id))
                {
                    existing.UserRoles.Add(new UserRole
                    {
                        UserId = existing.Id,
                        RoleId = studentRole.Id,
                        Role = studentRole,
                    });
                }
                existing.StudentProfile ??= new StudentProfile { UserId = existing.Id };
                existing.StudentProfile.HighSchool ??= "Trường THPT demo";
                existing.StudentProfile.Province ??= "Hà Nội";
                existing.StudentProfile.GraduationYear ??= 2026;
                existing.StudentProfile.InterestedSubjectGroup ??= "CMC-T2ANY";
                continue;
            }

            var user = new User
            {
                Email = email,
                PasswordHash = passwordHasher.Hash(defaultPassword),
                FullName = fullName,
                Phone = $"09{number % 100000000:00000000}",
                EmailVerifiedAt = DateTime.UtcNow,
            };

            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = studentRole.Id,
                Role = studentRole,
            });

            user.StudentProfile = new StudentProfile
            {
                UserId = user.Id,
                HighSchool = "Trường THPT demo",
                Province = "Hà Nội",
                GraduationYear = 2026,
                InterestedSubjectGroup = "CMC-T2ANY",
            };

            dbContext.Users.Add(user);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string BuildStudentFullName(int index)
    {
        string[] familyNames =
        [
            "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng",
            "Phan", "Vũ", "Đặng", "Bùi", "Đỗ"
        ];
        string[] middleNames =
        [
            "Minh", "Gia", "Khánh", "Tuấn", "Quang",
            "Anh", "Bảo", "Nhật", "Thùy", "Ngọc"
        ];
        string[] givenNames =
        [
            "An", "Bình", "Chi", "Dũng", "Hà",
            "Huy", "Linh", "Long", "Mai", "Trang"
        ];

        var family = familyNames[index % familyNames.Length];
        var middle = middleNames[(index / familyNames.Length) % middleNames.Length];
        var given = givenNames[index % givenNames.Length];
        return $"{family} {middle} {given}";
    }

    private async Task SeedCmcAdmissionsAsync(CancellationToken cancellationToken)
    {
        await DeactivateOldDemoAdmissionsAsync(cancellationToken);

        var cycle = await EnsureAdmissionCycleAsync(cancellationToken);
        var combinations = await EnsureSubjectCombinationsAsync(cancellationToken);
        await EnsureAdmissionMethodsAsync(cancellationToken);
        var faculties = await EnsureFacultiesAsync(cancellationToken);

        foreach (var programSeed in CmcPrograms)
        {
            var faculty = faculties[programSeed.FacultyCode];
            var major = await EnsureMajorAsync(faculty, programSeed, cancellationToken);
            var program = await EnsureProgramAsync(major, programSeed, combinations, cancellationToken);
            await EnsureTuitionAsync(program, programSeed, cancellationToken);
        }

        await EnsureFaqsAsync(cancellationToken);
        await RemoveLegacySeedKnowledgeDocumentAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DeactivateOldDemoAdmissionsAsync(CancellationToken cancellationToken)
    {
        var cmcCodes = CmcPrograms.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var program in await dbContext.Programs.ToListAsync(cancellationToken))
        {
            if (!cmcCodes.Contains(program.Code))
            {
                program.Status = "inactive";
            }
        }

        foreach (var major in await dbContext.Majors.ToListAsync(cancellationToken))
        {
            if (!cmcCodes.Contains(major.Code))
            {
                major.Status = "inactive";
            }
        }

        foreach (var faculty in await dbContext.Faculties.ToListAsync(cancellationToken))
        {
            if (!CmcFacultyCodes.Contains(faculty.Code))
            {
                faculty.Status = "inactive";
            }
        }

        foreach (var cycle in await dbContext.AdmissionCycles.ToListAsync(cancellationToken))
        {
            cycle.Status = cycle.Year == 2026 ? "active" : "inactive";
        }

        foreach (var faq in await dbContext.FaqItems.ToListAsync(cancellationToken))
        {
            if (faq.Category is "test" or "demo")
            {
                faq.Status = "inactive";
            }
        }

        foreach (var document in await dbContext.KnowledgeDocuments
            .Include(x => x.Versions)
            .ThenInclude(x => x.Chunks)
            .ToListAsync(cancellationToken))
        {
            var isCmcDocument = document.Title.Contains("CMCU", StringComparison.OrdinalIgnoreCase)
                || document.Title.Contains("Đại học CMC", StringComparison.OrdinalIgnoreCase)
                || (document.Source?.Contains("cmcu.edu.vn", StringComparison.OrdinalIgnoreCase) ?? false);

            if (isCmcDocument)
            {
                continue;
            }

            document.Status = "inactive";
            document.UpdatedAt = DateTime.UtcNow;
            foreach (var chunk in document.Versions.SelectMany(x => x.Chunks))
            {
                chunk.IsActive = false;
            }
        }
    }

    private async Task<AdmissionCycle> EnsureAdmissionCycleAsync(CancellationToken cancellationToken)
    {
        var cycle = await dbContext.AdmissionCycles.FirstOrDefaultAsync(x => x.Year == 2026, cancellationToken);
        if (cycle is null)
        {
            cycle = new AdmissionCycle
            {
                Year = 2026,
                StartDate = new DateOnly(2026, 3, 1),
                EndDate = new DateOnly(2026, 6, 30),
            };
            dbContext.AdmissionCycles.Add(cycle);
        }

        cycle.Name = "Tuyển sinh đại học chính quy CMCU 2026";
        cycle.Status = "active";
        return cycle;
    }

    private async Task<Dictionary<string, SubjectCombination>> EnsureSubjectCombinationsAsync(CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            ("CMC-T2ANY", "Toán x 2 + 2 môn bất kỳ", "Áp dụng cho nhóm Máy tính và Công nghệ thông tin."),
            ("CMC-T2LY", "Toán x 2 + Vật lý + 1 môn bất kỳ", "Áp dụng cho ngành Công nghệ Kỹ thuật Điện tử - Viễn thông."),
            ("CMC-T2HOA", "Toán x 2 + Hóa học + 1 môn bất kỳ", "Áp dụng cho ngành Công nghệ Kỹ thuật Điện tử - Viễn thông."),
            ("CMC-V2ANY", "Ngữ văn x 2 + 2 môn bất kỳ", "Áp dụng cho các nhóm ngành ngoài khối công nghệ theo thông tin tuyển sinh CMCU 2026."),
        };

        var result = new Dictionary<string, SubjectCombination>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, subjects, description) in seeds)
        {
            var item = await dbContext.SubjectCombinations.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
            if (item is null)
            {
                item = new SubjectCombination { Code = code };
                dbContext.SubjectCombinations.Add(item);
            }

            item.Subjects = subjects;
            item.Description = description;
            result[code] = item;
        }

        return result;
    }

    private async Task EnsureAdmissionMethodsAsync(CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            ("CMC401", "Xét kết quả kỳ thi Đánh giá năng lực CMC-TEST", "Bài thi trắc nghiệm trên máy tính gồm Toán học, Tiếng Anh và Tư duy logic."),
            ("CMC200", "Xét kết quả học tập THPT", "Xét điểm trung bình môn theo tổ hợp xét tuyển; thang điểm 40 theo thông tin tuyển sinh CMCU 2026."),
            ("CMC100", "Xét kết quả thi tốt nghiệp THPT", "Xét điểm thi tốt nghiệp THPT, có cộng điểm ưu tiên nếu có."),
            ("CMC303", "Xét tuyển thẳng", "Xét tuyển thẳng theo quy chế của Bộ GD&ĐT và đề án tuyển sinh của Trường Đại học CMC."),
        };

        foreach (var (code, name, description) in seeds)
        {
            var item = await dbContext.AdmissionMethods.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
            if (item is null)
            {
                item = new AdmissionMethod { Code = code };
                dbContext.AdmissionMethods.Add(item);
            }

            item.Name = name;
            item.Description = description;
            item.Status = "active";
        }
    }

    private async Task<Dictionary<string, Faculty>> EnsureFacultiesAsync(CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            ("CMC-ICT", "Khoa Công nghệ thông tin & Truyền thông", "Đào tạo nhóm Máy tính và Công nghệ thông tin của Trường Đại học CMC."),
            ("CMC-ENG", "Khoa Vi điện tử & Viễn thông", "Đào tạo Công nghệ Kỹ thuật Điện tử - Viễn thông, định hướng thiết kế vi mạch bán dẫn."),
            ("CMC-BUS", "Khoa Kinh doanh & Quản lý", "Đào tạo quản trị, logistics, marketing, thương mại điện tử và kinh doanh quốc tế."),
            ("CMC-MEDIA", "Khoa Truyền thông đa phương tiện", "Đào tạo truyền thông đa phương tiện và quan hệ công chúng."),
            ("CMC-ART", "Khoa Mỹ thuật và Thiết kế", "Đào tạo thiết kế đồ họa, đồ họa game và thiết kế mỹ thuật số."),
            ("CMC-LANG", "Khoa Ngôn ngữ", "Đào tạo Ngôn ngữ Hàn Quốc, Ngôn ngữ Trung Quốc và Tiếng Trung thương mại."),
        };

        var result = new Dictionary<string, Faculty>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, name, description) in seeds)
        {
            var item = await dbContext.Faculties.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
            if (item is null)
            {
                item = new Faculty { Code = code };
                dbContext.Faculties.Add(item);
            }

            item.Name = name;
            item.Description = description;
            item.Status = "active";
            result[code] = item;
        }

        return result;
    }

    private async Task<Major> EnsureMajorAsync(Faculty faculty, CmcProgramSeed seed, CancellationToken cancellationToken)
    {
        var item = await dbContext.Majors.FirstOrDefaultAsync(x => x.Code == seed.Code, cancellationToken);
        if (item is null)
        {
            item = new Major { Code = seed.Code };
            dbContext.Majors.Add(item);
        }

        item.Faculty = faculty;
        item.FacultyId = faculty.Id;
        item.Name = seed.DisplayName;
        item.Description = BuildMajorDescription(seed);
        item.CareerOutcomes = BuildCareerOutcomes(seed);
        item.Status = "active";
        item.UpdatedAt = DateTime.UtcNow;
        return item;
    }

    private async Task<AcademicProgram> EnsureProgramAsync(
        Major major,
        CmcProgramSeed seed,
        IReadOnlyDictionary<string, SubjectCombination> combinations,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.Programs
            .Include(x => x.SubjectCombinations)
            .FirstOrDefaultAsync(x => x.Code == seed.Code, cancellationToken);

        if (item is null)
        {
            item = new AcademicProgram { Code = seed.Code };
            dbContext.Programs.Add(item);
        }

        item.Major = major;
        item.MajorId = major.Id;
        item.Name = seed.DisplayName;
        item.DegreeType = "Đại học chính quy";
        item.Language = "Tiếng Việt";
        item.Campus = seed.HoChiMinhQuota is null ? "Hà Nội" : "Hà Nội; TP. Hồ Chí Minh";
        item.DurationYears = 4.5m;
        item.Description = BuildProgramDescription(seed);
        item.Status = "active";
        item.UpdatedAt = DateTime.UtcNow;

        string[] desiredCombinationCodes = seed.SubjectGroup switch
        {
            "TECH" => ["CMC-T2ANY"],
            "EC" => ["CMC-T2LY", "CMC-T2HOA"],
            _ => ["CMC-T2ANY", "CMC-V2ANY"],
        };

        var desiredIds = desiredCombinationCodes.Select(code => combinations[code].Id).ToHashSet();
        item.SubjectCombinations.Clear();
        foreach (var subjectId in desiredIds)
        {
            item.SubjectCombinations.Add(new ProgramSubjectCombination
            {
                Program = item,
                ProgramId = item.Id,
                SubjectCombinationId = subjectId,
            });
        }

        return item;
    }

    private async Task EnsureTuitionAsync(AcademicProgram program, CmcProgramSeed seed, CancellationToken cancellationToken)
    {
        await EnsureTuitionLineAsync(program, "2026 - Học kỳ 1-3", seed.TuitionSemester1To3, "Học phí/kỳ giai đoạn học kỳ 1-3 theo bảng học phí CMCU 2026.", cancellationToken);
        await EnsureTuitionLineAsync(program, "2026 - Học kỳ 4-6", seed.TuitionSemester4To6, "Học phí/kỳ giai đoạn học kỳ 4-6 theo bảng học phí CMCU 2026.", cancellationToken);
        await EnsureTuitionLineAsync(program, "2026 - Học kỳ 7-9", seed.TuitionSemester7To9, "Học phí/kỳ giai đoạn học kỳ 7-9 theo bảng học phí CMCU 2026.", cancellationToken);
    }

    private async Task EnsureTuitionLineAsync(AcademicProgram program, string academicYear, decimal amount, string note, CancellationToken cancellationToken)
    {
        var item = await dbContext.TuitionFees.FirstOrDefaultAsync(x => x.ProgramId == program.Id && x.AcademicYear == academicYear, cancellationToken);
        if (item is null)
        {
            item = new TuitionFee { Program = program, ProgramId = program.Id, AcademicYear = academicYear };
            dbContext.TuitionFees.Add(item);
        }

        item.AmountMin = amount;
        item.AmountMax = amount;
        item.Currency = "VND";
        item.Unit = "học kỳ";
        item.Note = note;
    }

    private async Task EnsureFaqsAsync(CancellationToken cancellationToken)
    {
        var faqs = new[]
        {
            ("truong", "Hệ thống này tư vấn cho trường nào?", "Hệ thống được cấu hình cho Trường Đại học CMC, mã trường CMC, dùng dữ liệu tuyển sinh chính quy năm 2026 từ nguồn công khai của CMCU."),
            ("thanh_lap", "Trường Đại học CMC thành lập năm nào?", "Trường Đại học CMC chính thức được đổi tên ngày 26/07/2022 theo Quyết định số 895/QĐ-TTg của Thủ tướng Chính phủ. Khi trả lời ngắn gọn có thể nói mốc chính thức là năm 2022."),
            ("giang_vien", "Đội ngũ giảng viên Trường Đại học CMC có những ai?", "Một số giảng viên, chuyên gia tiêu biểu được công bố gồm PGS. TS. Nguyễn Thanh Tùng - Hiệu trưởng; PGS. TS. Nguyễn Hữu Quỳnh - Phó Hiệu trưởng; PGS. TS. Vũ Việt Vũ - Trưởng Khoa Công nghệ Thông tin & Truyền thông; PGS. TS. Trương Anh Hoàng, TS. Phạm Thị Anh Lê, TS. Hoàng Tiểu Bình, TS. Ngô Minh Thành, TS. Nguyễn Ngọc Tân; TS. Lê Tiến Trung - Trưởng Khoa Kinh doanh & Quản lý; TS. Đặng Minh Tuấn - Trưởng Khoa Vi điện tử & Viễn thông; PGS. TS. Nguyễn Việt Dũng - Trưởng Khoa Đại cương."),
            ("lien_he", "Thông tin liên hệ và cơ sở của Trường Đại học CMC là gì?", "Tuyển sinh: tuyensinh@cmcu.edu.vn, 024 7102 9999. Trụ sở chính: CMC Tower, số 11 Duy Tân, Cầu Giấy, Hà Nội. Cơ sở 1: số 84C Nguyễn Thanh Bình, Hà Đông. Cơ sở 2: Vạn Phúc Building, đường Tố Hữu, Hà Đông. Cơ sở 3: Tây Mỗ, Xuân Phương, Hà Nội. Cơ sở Tân Thuận: CMC Creative Space, đường số 19, Khu chế xuất Tân Thuận, phường Tân Thuận, TP. Hồ Chí Minh."),
            ("phuong_thuc", "Đại học CMC có những phương thức xét tuyển nào năm 2026?", "Năm 2026, Trường Đại học CMC công bố 4 phương thức: CMC401 xét CMC-TEST, CMC200 xét kết quả học tập THPT, CMC100 xét kết quả thi tốt nghiệp THPT và CMC303 xét tuyển thẳng."),
            ("ho_so", "Hồ sơ xét tuyển trực tuyến Đại học CMC gồm những gì?", "Hồ sơ trực tuyến gồm bản PDF hoặc ảnh kết quả học tập THPT, CCCD, chứng chỉ ngoại ngữ/chứng nhận ưu tiên nếu có, bằng tốt nghiệp THPT đối với thí sinh đã tốt nghiệp trước năm 2026 và giấy chứng nhận thành tích nếu đăng ký xét tuyển thẳng."),
            ("hoc_phi", "Học phí Đại học CMC năm 2026 được tính như thế nào?", "Học phí CMCU 2026 được công bố theo từng học kỳ. Nhóm Máy tính và Công nghệ thông tin cùng Công nghệ kỹ thuật có mức 14.742.000, 18.018.000 và 21.840.000 VNĐ/kỳ theo các giai đoạn học kỳ 1-3, 4-6, 7-9. Nhóm Kinh doanh, Truyền thông, Nghệ thuật có mức 13.608.000, 16.632.000 và 20.160.000 VNĐ/kỳ. Nhóm Ngôn ngữ có mức 12.474.000, 15.246.000 và 18.480.000 VNĐ/kỳ."),
            ("chi_tieu", "Chỉ tiêu tuyển sinh Đại học CMC năm 2026 là bao nhiêu?", "Bảng ngành/chương trình tuyển sinh 2026 ghi tổng chỉ tiêu 2.315, trong khi một ô tổng quan trên cùng trang ghi 2.300. Thí sinh nên xác nhận với Phòng Tuyển sinh nếu cần dùng con số trong hồ sơ chính thức."),
            ("diem_chuan", "Điểm chuẩn Đại học CMC năm 2026 đã có chưa?", "Ngày 10/07/2026, Trường Đại học CMC đã công bố điểm sàn nộp hồ sơ và bảng quy đổi giữa các phương thức. Điểm sàn không phải điểm chuẩn trúng tuyển; hệ thống không tự bịa điểm chuẩn khi chưa có thông báo chính thức."),
            ("le_phi", "Lệ phí xét tuyển Đại học CMC năm 2026 là bao nhiêu?", "Theo thông tin tuyển sinh CMCU 2026, phí đăng ký thi CMC-TEST là miễn phí, phí đăng ký xét tuyển là 50.000 VNĐ/thí sinh và phí giữ học bổng, ưu đãi là 5.000.000 VNĐ/thí sinh nếu thuộc diện được cấp học bổng, ưu đãi."),
            ("thoi_gian", "Thời gian đăng ký hồ sơ Đại học CMC năm 2026 là khi nào?", "Với phương thức 1, 2 và 4, thí sinh đăng ký hồ sơ tại Trường Đại học CMC từ 01/03 đến 30/06/2026; đăng ký nguyện vọng trên hệ thống của Bộ GD&ĐT từ 02/07 đến 14/07/2026; xét bổ sung nếu có từ 22/08/2026."),
        };

        foreach (var (category, question, answer) in faqs)
        {
            var item = await dbContext.FaqItems.FirstOrDefaultAsync(x => x.Category == category, cancellationToken);
            if (item is null)
            {
                item = new FaqItem { Category = category };
                dbContext.FaqItems.Add(item);
            }

            item.Question = question;
            item.Answer = answer;
            item.Status = "active";
            item.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task RemoveLegacySeedKnowledgeDocumentAsync(CancellationToken cancellationToken)
    {
        const string title = "Nguồn tuyển sinh CMCU 2026 - bản tóm tắt seed";
        var legacyDocuments = await dbContext.KnowledgeDocuments
            .Where(x => x.Title == title)
            .ToListAsync(cancellationToken);
        dbContext.KnowledgeDocuments.RemoveRange(legacyDocuments);
    }

    private static string BuildMajorDescription(CmcProgramSeed seed)
    {
        var planned = seed.IsPlanned2026 ? " Đây là ngành/chương trình dự kiến mở năm 2026 theo bảng thông tin tuyển sinh CMCU." : "";
        var subtitle = string.IsNullOrWhiteSpace(seed.Subtitle) ? "" : $" Định hướng: {seed.Subtitle}.";
        var campuses = seed.HoChiMinhQuota is null ? "Hà Nội" : "Hà Nội và TP. Hồ Chí Minh";
        return $"{seed.DisplayName} thuộc nhóm {seed.GroupName} của Trường Đại học CMC và được công bố tuyển sinh tại {campuses}." +
               $"{subtitle}{planned} Chỉ tiêu chi tiết có thể được nhà trường điều chỉnh; cần đối chiếu bảng tuyển sinh chính thức mới nhất.";
    }

    private static string BuildProgramDescription(CmcProgramSeed seed)
    {
        var subject = seed.SubjectGroup switch
        {
            "TECH" => "Tổ hợp xét tuyển: Toán x 2 + 2 môn bất kỳ.",
            "EC" => "Tổ hợp xét tuyển: Toán x 2 + Lý + môn bất kỳ hoặc Toán x 2 + Hóa + môn bất kỳ.",
            _ => "Tổ hợp xét tuyển: Toán x 2 + 2 môn bất kỳ hoặc Văn x 2 + 2 môn bất kỳ.",
        };

        return $"{seed.DisplayName} - mã xét tuyển {seed.Code}. {subject} Học phí/kỳ năm 2026 lần lượt theo giai đoạn học kỳ 1-3, 4-6, 7-9 là {seed.TuitionSemester1To3:n0}, {seed.TuitionSemester4To6:n0}, {seed.TuitionSemester7To9:n0} VNĐ.";
    }

    private static string BuildCareerOutcomes(CmcProgramSeed seed)
    {
        return seed.SubjectGroup switch
        {
            "TECH" => "Lập trình viên, kỹ sư phần mềm, chuyên viên dữ liệu/AI, chuyên viên an toàn thông tin hoặc kỹ sư hệ thống tùy ngành học.",
            "EC" => "Kỹ sư điện tử - viễn thông, kỹ sư thiết kế vi mạch bán dẫn, kỹ sư hệ thống nhúng hoặc kỹ sư kiểm thử phần cứng.",
            "BUS" => "Chuyên viên kinh doanh, marketing, logistics, thương mại điện tử, truyền thông, thiết kế hoặc chuyên viên ngôn ngữ tùy chương trình.",
            _ => "Vị trí nghề nghiệp phụ thuộc định hướng chuyên ngành và năng lực cá nhân sau khi tốt nghiệp.",
        };
    }

    private sealed record CmcProgramSeed(
        string Code,
        string DisplayName,
        string FacultyCode,
        string GroupName,
        int HanoiQuota,
        int? HoChiMinhQuota,
        string SubjectGroup,
        decimal TuitionSemester1To3,
        decimal TuitionSemester4To6,
        decimal TuitionSemester7To9,
        bool IsPlanned2026 = false,
        string? Subtitle = null);
}
