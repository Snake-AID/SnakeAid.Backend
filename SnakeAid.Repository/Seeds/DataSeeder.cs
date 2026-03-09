using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;

namespace SnakeAid.Repository.Seeds
{
    public static class DataSeeder
    {
        private static readonly GeometryFactory _geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        private class HospitalDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string ContactNumber { get; set; }
            public CoordinatesDto Coordinates { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        private class CoordinatesDto
        {
            public double Lat { get; set; }
            public double Lng { get; set; }
        }
        public static async Task SeedAsync(SnakeAidDbContext context)
        {
            // ==================================================================================
            // SEED FIRST AID GUIDELINES
            // ==================================================================================
            // No dependencies - seed first
            if (!context.FirstAidGuidelines.Any())
            {
                var guidelines = new List<FirstAidGuideline>
                {
                    // 1. SƠ CỨU CHUNG
                    new FirstAidGuideline {
                        Id = 1,
                        Name = "Sơ cứu cơ bản",
                        Summary = "Các bước an toàn cho mọi trường hợp bị rắn cắn.",
                        Content = new FirstAidContent {
                            Steps = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Di chuyển nhẹ nhàng rời xa khu vực có rắn.", MediaUrl = "https://www.wikihow.com/images/thumb/3/39/Treat-a-Rattlesnake-Bite-Step-1-Version-4.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-1-Version-4.jpg" },
                                new FirstAidStep { Text = "Nằm yên, giữ vết cắn thấp hơn tim, hít thở đều để giữ bình tĩnh.", MediaUrl = "https://www.wikihow.com/images/thumb/d/da/Treat-a-Rattlesnake-Bite-Step-3-Version-3.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-3-Version-3.jpg" },
                                new FirstAidStep { Text = "Rửa vết cắn nhẹ nhàng bằng nước sạch.", MediaUrl = "" },
                                new FirstAidStep { Text = "Dùng nẹp cố định lỏng tay/chân bị cắn.", MediaUrl = "https://benhvienhuulung.vn/images/upload/v4-460px-Treat-a-Snake-Bite-Step-10-Version-5.jpg" },
                                new FirstAidStep { Text = "Gọi hỗ trợ y tế sớm nhất có thể.", MediaUrl = "https://dichvuxecuuthuong115.com/upload/images/goi-cap-cuu-115.jpg" },
                                new FirstAidStep { Text = "Đưa đến bệnh viện ngay lập tức.", MediaUrl = "https://png.pngtree.com/png-vector/20240216/ourmid/pngtree-flat-hospital-icon-building-vector-png-image_11740947.png" }
                            },
                            Dos = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Tháo nhẫn, đồng hồ, đồ chật gần vết cắn.", MediaUrl = "https://www.wikihow.com/images/thumb/2/27/Treat-a-Rattlesnake-Bite-Step-5-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-5-Version-2.jpg.webp" },
                                new FirstAidStep { Text = "Chụp ảnh rắn nếu an toàn", MediaUrl = "" }
                            },
                            Donts = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Không rạch, hút nọc", MediaUrl = "https://www.wikihow.com/images/thumb/7/75/Treat-a-Rattlesnake-Bite-Step-15-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-15-Version-2.jpg.webp" },
                                new FirstAidStep { Text = "Không chườm đá, đắp lá", MediaUrl = "https://cdn2.tuoitre.vn/zoom/480_300/1200/900/ttc/r/2021/07/15/image001-1626321686.jpg" },
                                new FirstAidStep { Text = "Không uống rượu/bia", MediaUrl = "https://www.mediplus.vn/wp-content/uploads/2021/10/truoc-khi-xet-nghiem-covid-19-duoc-uong-ruou-khong.jpg" }
                            }
                        }
                    },

                    // 2. ĐỘC THẦN KINH
                    new FirstAidGuideline {
                        Id = 2,
                        Name = "Sơ cứu Độc thần kinh",
                        Summary = "Ngăn chặn liệt hô hấp bằng cách băng cố định đúng cách.",
                        Content = new FirstAidContent {
                            Steps = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Di chuyển nhẹ nhàng rời khỏi khu vực có rắn.", MediaUrl = "https://www.wikihow.com/images/thumb/3/39/Treat-a-Rattlesnake-Bite-Step-1-Version-4.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-1-Version-4.jpg" },
                                new FirstAidStep { Text = "Gọi hỗ trợ y tế sớm nhất có thể.", MediaUrl = "https://dichvuxecuuthuong115.com/upload/images/goi-cap-cuu-115.jpg" },
                                new FirstAidStep { Text = "Quấn băng thun quanh vết cắn và toàn bộ tay/chân (chặt như băng bong gân).", MediaUrl = "" },
                                new FirstAidStep { Text = "Quấn từ ngón tay/chân đi ngược dần lên phía nách hoặc háng.", MediaUrl = "" },
                                new FirstAidStep { Text = "Dùng nẹp cố định để tay/chân không thể cử động.",
                                MediaUrl = "https://hscc.vn/hinhanh/randoccan_socuu.png" },
                                new FirstAidStep { Text = "Nằm yên và di chuyển bằng cáng. Tuyệt đối không tự đi bộ.", MediaUrl = "https://lh5.googleusercontent.com/i8pGvoLht7TcUieukFfbJgxyhfSjBKKh6HgjaBdOG949U2qn7JdQ4HApvHFebdFG5zpP2nrNwCfESg2yzAqZXSXW_aOXRe_lnsSeBgfyTtIXbiIBOiTUj4kvlVcPiqFDY6xOz0w"}
                            },
                            Dos = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Kiểm tra mạch ngọn chi (đảm bảo máu vẫn lưu thông)", MediaUrl = "" },
                                new FirstAidStep { Text = "Giữ nguyên băng quấn cho tới khi gặp bác sĩ", MediaUrl = "https://www.wikihow.com/images/thumb/f/fa/Treat-a-Rattlesnake-Bite-Step-9-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-9-Version-2.jpg" }
                            },
                            Donts = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Tuyệt đối không tự ý tháo băng quấn", MediaUrl = "https://www.wikihow.com/images/thumb/9/90/Treat-a-Rattlesnake-Bite-Step-8-Version-3.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-8-Version-3.jpg.webp" },
                                new FirstAidStep { Text = "Không để nạn nhân cử động tay chân", MediaUrl = "https://www.wikihow.com/images/thumb/6/68/Treat-a-Rattlesnake-Bite-Step-4-Version-4.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-4-Version-4.jpg" }
                            },
                            Notes = new List<string> { "Cảnh báo: Độc này có thể gây liệt cơ thở rất nhanh. Chú ý hỗ trợ hô hấp kịp thời." }
                        }
                    },

                    // 3. ĐỘC MÁU
                    new FirstAidGuideline {
                        Id = 3,
                        Name = "Sơ cứu Độc máu",
                        Summary = "Ngăn chảy máu và bảo vệ hệ tuần hoàn. (Không được quấn chặt)",
                        Content = new FirstAidContent {
                            Steps = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Rửa vết cắn nhẹ nhàng bằng nước sạch.", MediaUrl = "" },
                                new FirstAidStep { Text = "Dùng nẹp cố định tay/chân nhưng quấn lỏng tay (tuyệt đối không siết chặt).", MediaUrl = "" },
                                new FirstAidStep { Text = "Giữ vùng bị cắn nằm ngang mức với tim.", MediaUrl = "https://www.wikihow.com/images/thumb/d/da/Treat-a-Rattlesnake-Bite-Step-3-Version-3.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-3-Version-3.jpg" },
                                new FirstAidStep { Text = "Đưa nạn nhân đến bệnh viện khẩn cấp.", MediaUrl = "https://png.pngtree.com/png-vector/20240216/ourmid/pngtree-flat-hospital-icon-building-vector-png-image_11740947.png" }
                            },
                            Dos = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Tháo trang sức ngay (tránh sưng nề gây thắt mạch máu)", MediaUrl ="https://www.wikihow.com/images/thumb/2/27/Treat-a-Rattlesnake-Bite-Step-5-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-5-Version-2.jpg.webp" },
                                new FirstAidStep { Text = "Theo dõi các vết bầm tím", MediaUrl = "https://www.wikihow.com/images/thumb/c/cd/Treat-a-Rattlesnake-Bite-Step-11-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-11-Version-2.jpg" }
                            },
                            Donts = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Không băng ép chặt (Garrot)", MediaUrl = "https://www.wikihow.com/images/thumb/4/47/Treat-a-Rattlesnake-Bite-Step-16-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-16-Version-2.jpg" },
                                new FirstAidStep { Text = "Không dùng thuốc giảm đau như Aspirin", MediaUrl = "https://trungtamthuoc.com/images/products/aspirin-100-traphaco-l4575.jpg" }
                            },
                            Notes = new List<string> { "Dấu hiệu: Chảy máu chân răng, tiểu đỏ, nôn ra máu." }
                        }
                    },

                    // 4. ĐỘC TẾ BÀO
                    new FirstAidGuideline {
                        Id = 4,
                        Name = "Sơ cứu Độc tế bào",
                        Summary = "Ngăn ngừa thối rữa mô và hoại tử. (Không được quấn chặt)",
                        Content = new FirstAidContent {
                            Steps = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Rửa sạch vết thương và để thoáng mát.", MediaUrl = "https://png.pngtree.com/png-vector/20200325/ourlarge/pngtree-hand-wash-vector-icons-illustration-png-image_2164976.jpg" },
                                new FirstAidStep { Text = "Dùng nẹp cố định tay/chân nhưng quấn lỏng tay.", MediaUrl = "" },
                                new FirstAidStep { Text = "Giữ vùng bị cắn nằm ngang mức với tim.", MediaUrl = "" },
                                new FirstAidStep { Text = "Đưa đi cấp cứu sớm nhất có thể.", MediaUrl = "https://png.pngtree.com/png-vector/20240216/ourmid/pngtree-flat-hospital-icon-building-vector-png-image_11740947.png" }
                            },
                            Dos = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Tháo mọi vật gây thắt chi (nhẫn, vòng)", MediaUrl = "https://www.wikihow.com/images/thumb/2/27/Treat-a-Rattlesnake-Bite-Step-5-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-5-Version-2.jpg.webp" },
                                new FirstAidStep { Text = "Theo dõi vùng da bị đổi màu hoặc phồng rộp", MediaUrl = "https://www.wikihow.com/images/thumb/c/cd/Treat-a-Rattlesnake-Bite-Step-11-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-11-Version-2.jpg" }
                            },
                            Donts = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Không chườm đá lạnh trực tiếp", MediaUrl = "" },
                                new FirstAidStep { Text = "Không quấn băng chặt quanh vết cắn (nhanh hoại tử)", MediaUrl = "https://www.wikihow.com/images/thumb/4/47/Treat-a-Rattlesnake-Bite-Step-16-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-16-Version-2.jpg" }
                            },
                            Notes = new List<string> { "Dấu hiệu: Vết cắn sưng vù rất nhanh, da thâm đen." }
                        }
                    },

                    // 5. ĐỘC CƠ
                    new FirstAidGuideline {
                        Id = 5,
                        Name = "Sơ cứu Độc cơ",
                        Summary = "Bảo vệ cơ bắp và ngăn suy thận cấp. (Cần quấn băng + uống nước)",
                        Content = new FirstAidContent {
                            Steps = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Quấn băng thun quanh vết cắn và toàn bộ tay/chân (chặt như băng bong gân).", MediaUrl = "" },
                                new FirstAidStep { Text = "Quấn từ ngón tay/chân đi ngược dần lên phía nách hoặc háng.", MediaUrl = "https://hscc.vn/hinhanh/randoccan_socuu.png" },
                                new FirstAidStep { Text = "Uống thật nhiều nước (nếu còn tỉnh táo) để giúp thận thải độc.", MediaUrl = "https://karofi.karofi.com/karofi-com/2019/12/uong-nuoc-karofi-2.jpg.webp" },
                                new FirstAidStep { Text = "Vận chuyển bằng cáng đến bệnh viện có máy lọc thận gấp.", MediaUrl = "https://png.pngtree.com/png-vector/20240216/ourmid/pngtree-flat-hospital-icon-building-vector-png-image_11740947.png" }
                            },
                            Dos = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Giữ ấm cơ thể nạn nhân", MediaUrl = "" },
                                new FirstAidStep { Text = "Theo dõi màu nước tiểu", MediaUrl = "" }
                            },
                            Donts = new List<FirstAidStep> {
                                new FirstAidStep { Text = "Không vận động cơ bắp", MediaUrl = "https://lh5.googleusercontent.com/i8pGvoLht7TcUieukFfbJgxyhfSjBKKh6HgjaBdOG949U2qn7JdQ4HApvHFebdFG5zpP2nrNwCfESg2yzAqZXSXW_aOXRe_lnsSeBgfyTtIXbiIBOiTUj4kvlVcPiqFDY6xOz0w" },
                                new FirstAidStep { Text = "Không dùng thuốc giảm đau bừa bãi", MediaUrl = "https://trungtamthuoc.com/images/products/aspirin-100-traphaco-l4575.jpg" }
                            },
                            Notes = new List<string> { "Dấu hiệu: Đau nhức cơ toàn thân, nước tiểu màu nâu/đen như xá xị." }
                        }
                    }
                };
                context.FirstAidGuidelines.AddRange(guidelines);
                await context.SaveChangesAsync();
            }

            // ==================================================================================
            // SEED VENOM TYPES
            // ==================================================================================
            // Depends on: FirstAidGuidelines
            if (!context.VenomTypes.Any())
            {
                var venomTypes = new List<VenomType>
                {
                    new VenomType
                    {
                        Id = 1,
                        Name = "Độc thần kinh",
                        ScientificName = "Độc thần kinh",
                        Description = "Nọc độc chủ yếu ảnh hưởng đến hệ thần kinh, gây tê liệt và suy hô hấp.",
                        IsActive = true,
                        SeverityIndex = 9,
                        FirstAidGuidelineId = 2
                    },
                    new VenomType
                    {
                        Id = 2,
                        Name = "Độc máu",
                        ScientificName = "Độc máu",
                        Description = "Nọc độc gây tổn thương mạch máu và mô, dẫn đến chảy máu và tổn thương cơ quan.",
                        IsActive = true,
                        SeverityIndex = 8,
                        FirstAidGuidelineId = 3
                    },
                    new VenomType
                    {
                        Id = 3,
                        Name = "Độc tế bào",
                        ScientificName = "Độc tế bào",
                        Description = "Nọc độc phá hủy tế bào và mô tại vị trí cắn, gây tổn thương nghiêm trọng tại chỗ.",
                        IsActive = true,
                        SeverityIndex = 7,
                        FirstAidGuidelineId = 4
                    },
                    new VenomType
                    {
                        Id = 4,
                        Name = "Độc cơ",
                        ScientificName = "Độc cơ",
                        Description = "Nọc độc gây tổn thương mô cơ, dẫn đến tiêu cơ.",
                        IsActive = true,
                        SeverityIndex = 6,
                        FirstAidGuidelineId = 5
                    }
                };
                context.VenomTypes.AddRange(venomTypes);
                await context.SaveChangesAsync();
            }

            // ==================================================================================
            // SEED FILTER QUESTIONS
            // ==================================================================================
            // No dependencies
            if (!context.FilterQuestions.Any())
            {
                var filterQuestions = new List<FilterQuestion>
                {
                    new FilterQuestion { Id = 1, Question = "Bạn gặp rắn ở khu vực nào?", IsActive = true },
                    new FilterQuestion { Id = 2, Question = "Bạn tìm thấy con rắn ở đâu?", IsActive = true },
                    new FilterQuestion { Id = 3, Question = "Hình dạng đầu của rắn?", IsActive = true },
                    new FilterQuestion { Id = 4, Question = "Màu sắc chủ đạo trên thân?", IsActive = true },
                    new FilterQuestion { Id = 5, Question = "Hoa văn trên lưng rắn?", IsActive = true },
                    new FilterQuestion { Id = 6, Question = "Đặc điểm nổi bật khác?", IsActive = true }
                };
                context.FilterQuestions.AddRange(filterQuestions);
                await context.SaveChangesAsync();
            }

            // ==================================================================================
            // SEED FILTER OPTIONS
            // ==================================================================================
            // Depends on: FilterQuestions
            if (!context.FilterOptions.Any())
            {
                var filterOptions = new List<FilterOption>
                {
                        // --- Câu hỏi 1: Vùng miền  ---
                        new FilterOption { Id = 1, QuestionId = 1, OptionText = "Miền Bắc", OptionImageUrl = "https://revovietnam.com/public/upload/baiviet/image_gallery.gif" },
                        new FilterOption { Id = 2, QuestionId = 1, OptionText = "Miền Trung / Tây Nguyên", OptionImageUrl = "https://revovietnam.com/public/upload/images/thumb_baiviet/mien-trung-201513584999.jpg" },
                        new FilterOption { Id = 3, QuestionId = 1, OptionText = "Miền Nam", OptionImageUrl = "https://revovietnam.com/public/upload/images/thumb_baiviet/mien-tay-571513585038.jpg" },

                        // --- Câu hỏi 2: Nơi gặp rắn (Id: 2) ---
                        new FilterOption { Id = 4, QuestionId = 2, OptionText = "Trong nhà / Khu dân cư (Kho bãi, vườn nhà)", OptionImageUrl = "https://img.pikbest.com/png-images/qiantu/vector-green-house-icon-png-illustration_2622666.png!sw800" },
                        new FilterOption { Id = 5, QuestionId = 2, OptionText = "Dưới nước (Sông, suối, ao, hồ, biển)", OptionImageUrl = "https://png.pngtree.com/png-vector/20220106/ourmid/pngtree-cartoon-blue-curved-river-png-element-png-image_4087313.png" },
                        new FilterOption { Id = 6, QuestionId = 2, OptionText = "Trên cây / Bụi rậm rậm rạp", OptionImageUrl = "https://img.pikbest.com/origin/09/29/21/15kpIkbEsT64F.png!sw800" },
                        new FilterOption { Id = 7, QuestionId = 2, OptionText = "Đồng ruộng / Bãi cỏ trống", OptionImageUrl = "https://media.istockphoto.com/id/668003824/vi/vec-to/c%C3%A1nh-%C4%91%E1%BB%93ng-l%C3%BAa-vect%C6%A1.jpg?s=612x612&w=0&k=20&c=1wVmiQH_aX0jE6wSfr0nolPaOKS3jMXKWd7svoltqbM=" },
                        new FilterOption { Id = 8, QuestionId = 2, OptionText = "Hang hốc / Dưới lớp lá khô / Đất đá", OptionImageUrl = "https://thumb.photo-ac.com/e5/e5b79797d908873974e0135044aff1c5_t.jpeg" },

                        // --- Câu hỏi 2: Hình dạng đầu  ---
                        new FilterOption { Id = 9, QuestionId = 3, OptionText = "Đầu hình tam giác / trái tim (phình to 2 bên)", OptionImageUrl = "https://cly.1cdn.vn/2020/12/26/s1.media.ngoisao.vn-resize_660-news-2020-12-22-_cach-nhan-biet-ran-doc-3-ngoisaovn-w728-h536.jpg" },
                        new FilterOption { Id = 10, QuestionId = 3, OptionText = "Đầu thon dài / bầu dục (không rõ cổ)", OptionImageUrl = "https://cly.1cdn.vn/2020/12/26/s1.media.ngoisao.vn-resize_660-news-2020-12-22-_cach-nhan-biet-ran-doc-3-ngoisaovn-w728-h536.jpg" },

                        // --- Câu hỏi 3: Màu sắc chủ đạo  ---
                        new FilterOption { Id = 11, QuestionId = 4, OptionText = "Xanh lá cây", OptionImageUrl = "media/filters/color_green.jpg" },
                        new FilterOption { Id = 12, QuestionId = 4, OptionText = "Đen hoặc Nâu đậm", OptionImageUrl = "media/filters/color_black_brown.jpg" },
                        new FilterOption { Id = 13, QuestionId = 4, OptionText = "Xám hoặc Màu đất", OptionImageUrl = "media/filters/color_grey.jpg" },
                        new FilterOption { Id = 14, QuestionId = 4, OptionText = "Vàng hoặc Cam", OptionImageUrl = "media/filters/color_yellow_orange.jpg" },

                        // --- Câu hỏi 4: Hoa văn  ---
                        
                        new FilterOption { Id = 15, QuestionId = 5, OptionText = "Thân trơn (một màu, không hoa văn)", OptionImageUrl = "https://em-content.zobj.net/source/joypixels/291/snake_1f40d.png" },
                        new FilterOption { Id = 16, QuestionId = 5, OptionText = "Khoanh tròn (vòng quanh thân)", OptionImageUrl = "https://png.pngtree.com/png-clipart/20200225/original/pngtree-red-snake-with-blue-stripes-icon-isolated-png-image_5261159.jpg" },
                        new FilterOption { Id = 17, QuestionId = 5, OptionText = "Sọc dọc (chạy từ đầu đến đuôi)", OptionImageUrl = "https://pcs.com.vn/static/384/2022/08/30/39.png" },
                        new FilterOption { Id = 18, QuestionId = 5, OptionText = "Đốm hoặc Vân phức tạp (hình thoi, tam giác)", OptionImageUrl = "https://png.pngtree.com/png-vector/20191118/ourmid/pngtree-yellow-spotted-snake-icon-isolated-png-image_1999504.jpg" },

                        // --- Câu hỏi 5: Đặc điểm nổi bật  ---
                        new FilterOption { Id = 19, QuestionId = 6, OptionText = "Có khả năng phình mang ở cổ", OptionImageUrl = "https://png.pngtree.com/png-clipart/20210214/ourmid/pngtree-cobra-clipart-viper-cartoon-style-png-image_2906947.jpg" },
                        new FilterOption { Id = 20, QuestionId = 6, OptionText = "Đuôi có màu đỏ hoặc cam nổi bật", OptionImageUrl = "https://dalieudanang.com/assets/news/2014_11/ran.png" },
                        new FilterOption { Id = 21, QuestionId = 6, OptionText = "Cổ có màu đỏ hoặc vàng", OptionImageUrl = "https://pcs.com.vn/static/326/2022/08/29/25.png" },
                        new FilterOption { Id = 22, QuestionId = 6, OptionText = "Thân có vảy nhám / gồ ghề", OptionImageUrl = "https://khoahoc.tv/photos/image/022013/04/Trimeresuruscornutus.jpg" }
                };
                context.FilterOptions.AddRange(filterOptions);
                await context.SaveChangesAsync();
            }

            // ==================================================================================
            // SEED SNAKE SPECIES
            // ==================================================================================
            // No direct dependency
            if (!context.SnakeSpecies.Any())
            {
                var snakes = new List<SnakeSpecies>
                {
                    // 1. RẮN CẠP NIA BẮC (Bungarus multicinctus)
                    new SnakeSpecies
                    {
                        Id = 1,
                        ScientificName = "Bungarus multicinctus",
                        CommonName = "Rắn Cạp Nia Bắc",
                        Slug = "ran-cap-nia-bac",
                        Description = "Một trong những loài rắn độc nhất châu Á, thường gặp ở vùng đồng bằng và trung du Bắc Bộ.",
                        IdentificationSummary = "Chiều dài trung bình 1.0m - 1.5m. Thân có các khoanh trắng và đen rõ rệt, vảy trơn bóng.",
                        PrimaryVenomType = PrimaryVenomType.Neurotoxic,
                        RiskLevel = 9.5f,
                        IsVenomous = true,
                        ImageUrl = "https://e.khoahoc.tv/photos/image/2020/09/19/ran-cap-nia-1.jpg",
                        Identification = new IdentificationFeature
                        {
                            PhysicalTraits = new List<string> { "Chiều dài: 100 - 150 cm", "Khoanh trắng đen rõ rệt", "Đầu bầu dục", "Vảy bóng", "Thân hình tam giác nhẹ" },
                            Behaviors = new List<string> { "Hoạt động mạnh về đêm", "Thích nơi ẩm ướt", "Thường chui vào nhà dân tìm mồi" },
                            Habitat = "Cánh đồng, ven sông, khu dân cư miền Bắc"
                        },
                        SymptomsByTime = new List<SymptomTimeline>
                        {
                            new SymptomTimeline { TimeRange = "0 - 30 phút", Signs = new List<string> { "Đau nhẹ tại vết cắn, ít cảm giác", "Tê nhẹ" }, IsCritical = false },
                            new SymptomTimeline { TimeRange = "1 - 3 giờ", Signs = new List<string> { "Sụp mí mắt", "Khó nói", "Khó nuốt", "Yếu cơ" }, IsCritical = true },
                            new SymptomTimeline { TimeRange = "3 - 6 giờ", Signs = new List<string> { "Liệt cơ hô hấp", "Ngừng thở" }, IsCritical = true }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride
                        {
                            Mode = OverrideMode.Append,
                            Content = new FirstAidContent
                            {
                                Steps = new List<FirstAidStep>
                                {
                                    new FirstAidStep { Text = "Đặc biệt chú ý hỗ trợ hô hấp nhân tạo nếu nạn nhân có dấu hiệu ngưng thở." }
                                }
                            }
                        }
                    },

                    // 2. RẮN LỤC ĐUÔI ĐỎ (Trimeresurus albolabris)
                    new SnakeSpecies
                    {
                        Id = 2,
                        ScientificName = "Trimeresurus albolabris",
                        CommonName = "Rắn Lục Đuôi Đỏ",
                        Slug = "ran-luc-duoi-do",
                        Description = "Loài rắn lục phổ biến nhất, thường sống trên cây và gây ra nhiều vụ tai nạn tại Việt Nam.",
                        IdentificationSummary = "Chiều dài trung bình 60cm - 90cm. Thân màu xanh lá cây, đầu hình tam giác rõ rệt, chót đuôi có màu đỏ cam.",
                        PrimaryVenomType = PrimaryVenomType.Hemotoxic,
                        RiskLevel = 7.5f,
                        IsVenomous = true,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/32/1736395426_0.jpg",
                        Identification = new IdentificationFeature
                        {
                            PhysicalTraits = new List<string> { "Chiều dài: 60 - 90 cm", "Thân xanh lá", "Đuôi màu đỏ", "Đầu tam giác phình to", "Vảy nhám" },
                            Behaviors = new List<string> { "Sống trên cây", "Ngụy trang cực tốt trong lá cây", "Hay xuất hiện ở bụi hoa, vườn nhà" },
                            Habitat = "Vườn cây, bụi rậm, rừng thưa trên toàn quốc"
                        },
                        SymptomsByTime = new List<SymptomTimeline>
                        {
                            new SymptomTimeline { TimeRange = "0 - 15 phút", Signs = new List<string> { "Đau nhức dữ dội", "Sưng nề nhanh chóng" }, IsCritical = false },
                            new SymptomTimeline { TimeRange = "1 - 6 giờ", Signs = new List<string> { "Xuất hiện bọng nước", "Chảy máu không cầm", "Bầm tím diện rộng" }, IsCritical = true }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride
                        {
                            Mode = OverrideMode.Replace,
                            Content = new FirstAidContent
                            {
                                Steps = new List<FirstAidStep>
                                {
                                    new FirstAidStep { Text = "Rửa sạch vết thương." },
                                    new FirstAidStep { Text = "Bất động lỏng chi." },
                                    new FirstAidStep { Text = "TUYỆT ĐỐI KHÔNG BĂNG ÉP CHẶT vì gây hoại tử nhanh." }
                                }
                            }
                        }
                    },

                    // 3. RẮN HỔ MANG CHÚA (Ophiophagus hannah)
                    new SnakeSpecies
                    {
                        Id = 3,
                        ScientificName = "Ophiophagus hannah",
                        CommonName = "Rắn Hổ Mang Chúa",
                        Slug = "ran-ho-mang-chua",
                        Description = "Loài rắn độc dài nhất thế giới, cực kỳ nguy hiểm với lượng nọc độc khổng lồ.",
                        IdentificationSummary = "Kích thước khổng lồ (4-6m), Vảy đầu lớn, Cổ phình mang hẹp, vân chữ V ngược ở cổ.",
                        PrimaryVenomType = PrimaryVenomType.Neurotoxic,
                        RiskLevel = 10.0f,
                        IsVenomous = true,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/55/1737107747_0.jpg",
                        Identification = new IdentificationFeature
                        {
                            PhysicalTraits = new List<string> { "Kích thước khổng lồ (4-6m)",
                            "Cặp vảy chẩm hình cánh bướm, nằm ở ngay phía sau vảy đầu",
                            "Phình mang hẹp dài", "Màu đen, nâu hoặc vàng chì" },
                            Behaviors = new List<string> { "Chủ động tấn công nếu bị kích động", "Có khả năng rướn cao thân mình" ,"Là một loài rắn thông minh, sẽ quan sát và phản ứng." },
                            Habitat = "Rừng rậm, nương rẫy, gần nguồn nước"
                        },
                        SymptomsByTime = new List<SymptomTimeline>
                        {
                            new SymptomTimeline { TimeRange = "0 - 15 phút", Signs = new List<string> { "Đau nhức", "Chóng mặt", "Hoa mắt" }, IsCritical = true },
                            new SymptomTimeline { TimeRange = "30 - 60 phút", Signs = new List<string> { "Hôn mê", "Suy hô hấp cấp", "Tử vong nhanh nếu không cấp cứu" }, IsCritical = true }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride
                        {
                            Mode = OverrideMode.Append,
                            Content = new FirstAidContent
                            {
                                Steps = new List<FirstAidStep>
                                {
                                    new FirstAidStep { Text = "Vận chuyển nạn nhân bằng phương tiện nhanh nhất có thể đến bệnh viện lớn." }
                                }
                            }
                        }
                    },

                    // 4. RẮN RÁO (Ptyas korros) - KHÔNG ĐỘC
                    new SnakeSpecies
                    {
                        Id = 4,
                        ScientificName = "Ptyas korros",
                        CommonName = "Rắn Ráo",
                        Slug = "ran-rao",
                        Description = "Loài rắn không độc phổ biến, thường bị nhầm lẫn với rắn hổ mang.",
                        IdentificationSummary = "Dài trung bình 1,2 - 2,0m. Mắt rất lớn, thân thon dài màu nâu đất hoặc xám chì, di chuyển cực nhanh.",
                        PrimaryVenomType = PrimaryVenomType.None,
                        RiskLevel = 1.0f,
                        IsVenomous = false,
                        ImageUrl = "https://www.cakhotranluan.com/images/2022/2ran-rao1.jpg",
                        Identification = new IdentificationFeature
                        {
                            PhysicalTraits = new List<string> { "Chiều dài: 120 - 200 cm", "Mắt to", "Thân dài thon", "Vảy trơn bóng", "Đầu bầu dục" },
                            Behaviors = new List<string> { "Di chuyển rất nhanh", "Hoạt động ban ngày", "Thường chạy trốn khi gặp người" },
                            Habitat = "Đồng ruộng, bụi rậm, vườn nhà"
                        },
                        SymptomsByTime = new List<SymptomTimeline>
                        {
                            new SymptomTimeline { TimeRange = "Sau khi cắn", Signs = new List<string> { "Chảy máu nhẹ", "Vết xước li ti", "Không sưng nề" }, IsCritical = false }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride
                        {
                            Mode = OverrideMode.Append,
                            Content = new FirstAidContent
                            {
                                Steps = new List<FirstAidStep>
                                {
                                    new FirstAidStep { Text = "Sát trùng vết thương bằng cồn hoặc nước sạch." }
                                }
                            }
                        }
                    },

                    new SnakeSpecies
                    {
                        Id = 5,
                        ScientificName = "Bungarus fasciatus",
                        CommonName = "Rắn Cạp Nong",
                        Slug = "ran-cap-nong",
                        Description = "Loài rắn độc thần kinh nguy hiểm, dễ nhận biết với các khoanh vàng đen xen kẽ đều nhau.",
                        IdentificationSummary = "Kích thước 1.8 - 2.3m. Thân hình tam giác với sống lưng gồ cao, đầu có vệt vàng hình mũi tên.",
                        PrimaryVenomType = PrimaryVenomType.Neurotoxic,
                        RiskLevel = 9.0f,
                        IsVenomous = true,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/46/1736402914_0.jpg",
                        Identification = new IdentificationFeature
                        {
                            PhysicalTraits = new List<string> { "Khoanh đen và vàng xen kẽ đều nhau", "Thân hình tam giác, sống lưng gồ", "Vệt vàng hai bên má tạo hình mũi tên" },
                            Behaviors = new List<string> { "Săn mồi ban đêm", "Bị thu hút bởi ánh lửa", "Tính tình thường nhút nhát nhưng độc tính rất mạnh" },
                            Habitat = "Rừng núi, bụi rậm, ven nguồn nước"
                        },
                        SymptomsByTime = new List<SymptomTimeline>
                        {
                            new SymptomTimeline { TimeRange = "0 - 30 phút", Signs = new List<string> { "Ngứa nhẹ hoặc tê rát tại vết cắn", "Ít đau khiến nạn nhân chủ quan" }, IsCritical = false },
                            new SymptomTimeline { TimeRange = "1 - 2 giờ", Signs = new List<string> { "Mệt mỏi bất thường", "Tức ngực nhẹ", "Sụp mí mắt nhẹ" }, IsCritical = true },
                            new SymptomTimeline { TimeRange = "2 - 6 giờ", Signs = new List<string> { "Đau nhức toàn thân", "Yếu liệt cơ tiến triển", "Suy hô hấp cấp" }, IsCritical = true }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride
                        {
                            Mode = OverrideMode.Append,
                            Content = new FirstAidContent
                            {
                                Steps = new List<FirstAidStep>
                                {
                                    new FirstAidStep { Text = "Cảnh giác cao độ nếu bị cắn khi đang cắm trại hoặc đi rừng ban đêm." }
                                }
                            }
                        }
                    },

                    new SnakeSpecies
                    {
                        Id = 6,
                        ScientificName = "Bungarus candidus",
                        CommonName = "Rắn Cạp Nia Nam",
                        Slug = "ran-cap-nia-nam",
                        Description = "Loài rắn độc thần kinh cực mạnh ở miền Nam, có tập tính lẻn vào nhà người dân.",
                        IdentificationSummary = "Khoanh đen và trắng có độ rộng gần bằng nhau, thân tròn bóng, đầu nhỏ.",
                        PrimaryVenomType = PrimaryVenomType.Neurotoxic,
                        RiskLevel = 9.8f,
                        IsVenomous = true,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/38/1736401130_0.jpg",
                        Identification = new IdentificationFeature
                        {
                            PhysicalTraits = new List<string> { "Khoanh đen và trắng/vàng nhạt đều nhau", "Thân tròn, vảy trơn bóng", "Đầu nhỏ không phân biệt rõ với cổ" },
                            Behaviors = new List<string> { "Hoạt động đêm", "Hay chui vào nhà dân", "Cắn người khi đang ngủ" },
                            Habitat = "Vùng đồng bằng, khu dân cư miền Nam Việt Nam"
                        },
                        SymptomsByTime = new List<SymptomTimeline>
                        {
                            new SymptomTimeline { TimeRange = "0 - 15 phút", Signs = new List<string> { "Vết cắn rất nhẹ, không sưng đau", "Khó thấy dấu răng" }, IsCritical = false },
                            new SymptomTimeline { TimeRange = "2 - 3 giờ", Signs = new List<string> { "Sụp mí mắt nặng", "Đồng tử giãn", "Nói ngọng", "Yếu chi" }, IsCritical = true },
                            new SymptomTimeline { TimeRange = "3 - 6 giờ", Signs = new List<string> { "Liệt cơ toàn thân", "Suy hô hấp hoàn toàn" }, IsCritical = true }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride
                        {
                            Mode = OverrideMode.Append,
                            Content = new FirstAidContent
                            {
                                Steps = new List<FirstAidStep>
                                {
                                    new FirstAidStep { Text = "Tuyệt đối không chờ triệu chứng đau mới đi viện vì nọc cạp nia không gây đau." }
                                }
                            }
                        }
                    },

                    new SnakeSpecies
                    {
                        Id = 7,
                        ScientificName = "Naja kaouthia",
                        CommonName = "Rắn Hổ Mang Xiêm",
                        Slug = "ran-ho-mang-xiem",
                        Description = "Loài rắn hổ mang có nọc độc hỗn hợp, gây hoại tử mô nghiêm trọng và liệt thần kinh.",
                        IdentificationSummary = "Màu nâu xám hoặc đen, có bành cổ với một hình tròn đơn (hình kính mắt) ở mặt sau.",
                        PrimaryVenomType = PrimaryVenomType.Neurotoxic, // Lưu ý: Hỗn hợp thần kinh và tế bào
                        RiskLevel = 9.0f,
                        IsVenomous = true,
                        ImageUrl = "https://photo.znews.vn/w660/Uploaded/rotnrz/2023_07_04/ho_meo_1.jpg",
                        Identification = new IdentificationFeature
                        {
                            PhysicalTraits = new List<string> { "Bành cổ rộng", "Hình kính mắt một vòng tròn sau cổ", "Màu nâu hoặc xám đen" },
                            Behaviors = new List<string> { "Có thể phun nọc độc xa và chuẩn", "Ngóc đầu cao và phình mang khi tấn công" },
                            Habitat = "Ruộng lúa, vườn tược, khu dân cư gần nguồn nước"
                        },
                        SymptomsByTime = new List<SymptomTimeline>
                        {
                            new SymptomTimeline { TimeRange = "0 - 10 phút", Signs = new List<string> { "Đau rõ rệt", "Chảy máu tại vết cắn" }, IsCritical = false },
                            new SymptomTimeline { TimeRange = "10 - 60 phút", Signs = new List<string> { "Sưng nhanh", "Đau tăng dần", "Có thể phồng rộp da" }, IsCritical = false },
                            new SymptomTimeline { TimeRange = "1 - 3 giờ", Signs = new List<string> { "Sụp mí mắt", "Yếu cơ", "Khó nuốt" }, IsCritical = true },
                            new SymptomTimeline { TimeRange = "6 - 24 giờ", Signs = new List<string> { "Hoại tử mô tại chỗ", "Nhiễm trùng vết thương" }, IsCritical = true }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride
                        {
                            Mode = OverrideMode.Append,
                            Content = new FirstAidContent
                            {
                                Steps = new List<FirstAidStep>
                                {
                                    new FirstAidStep { Text = "Nếu bị nọc phun vào mắt, phải rửa bằng nước sạch liên tục 15-20 phút." }
                                }
                            }
                        }
                    },

                    new SnakeSpecies
                    {
                        Id = 8,
                        ScientificName = "Rhabdophis subminiatus",
                        CommonName = "Rắn Hoa Cỏ Cổ Đỏ",
                        Slug = "ran-hoa-co-co-do",
                        Description = "Loài rắn có độc nguy hiểm điều kiện (độc nanh sau và độc da vùng cổ), gây rối loạn đông máu nặng và chưa có huyết thanh đặc hiệu.",
                        IdentificationSummary = "Cổ màu đỏ rực hoặc cam vàng đặc trưng, thân xanh ô liu, mắt tròn lớn.",
                        PrimaryVenomType = PrimaryVenomType.Hemotoxic,
                        RiskLevel = 8.5f,
                        IsVenomous = true,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/93/1737024030_0.jpeg",
                        Identification = new IdentificationFeature
                        {
                            PhysicalTraits = new List<string> { "Vòng cổ màu đỏ rực hoặc vàng cam", "Đầu thuôn không hình tam giác", "Mắt tròn, đồng tử tròn" },
                            Behaviors = new List<string> { "Ngóc đầu, tiết độc trắng đục và bẹt cổ giống rắn hổ khi bị đe dọa", "Tính khí không ổn định (lúc hiền lúc dữ)" },
                            Habitat = "Rừng, nương rẫy, gần nguồn nước"
                        },
                        SymptomsByTime = new List<SymptomTimeline>
                        {
                            new SymptomTimeline {
                                TimeRange = "0 - 1 giờ",
                                Signs = new List<string> { "Vết cắn đau nhẹ", "Sưng nhẹ cục bộ", "Có thể không có cảm giác bị nhiễm độc ngay" },
                                IsCritical = false
                            },
                            new SymptomTimeline {
                                TimeRange = "1 - 6 giờ",
                                Signs = new List<string> { "Máu rỉ rả không cầm tại vết cắn", "Bầm tím lan rộng", "Đau bụng, buồn nôn" },
                                IsCritical = true
                            },
                            new SymptomTimeline {
                                TimeRange = "6 - 24 giờ",
                                Signs = new List<string> { "Chảy máu chân răng, máu cam", "Tiểu ra máu", "Nôn ra máu", "Dấu hiệu suy thận" },
                                IsCritical = true
                            }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride
                        {
                            Mode = OverrideMode.Replace, // Thay thế hoàn toàn vì cách tiếp cận điều trị rất khác
                            Content = new FirstAidContent
                            {
                                Steps = new List<FirstAidStep> {
                                    new FirstAidStep { Text = "Đặt nạn nhân nằm yên, bất động hoàn toàn chi bị cắn.", MediaUrl = "https://assets.snakeaid.vn/aid/immobilize.gif" },
                                    new FirstAidStep { Text = "Băng ép nhẹ bằng băng vải rộng để bảo vệ vết thương.", MediaUrl = "https://assets.snakeaid.vn/aid/light-bandage.jpg" },
                                    new FirstAidStep { Text = "Nhanh chóng chuyển nạn nhân đến bệnh viện tuyến tỉnh hoặc trung ương có khả năng lọc máu và truyền máu.", MediaUrl = "" }
                                },
                                Dos = new List<FirstAidStep> {
                                    new FirstAidStep { Text = "Báo cho bác sĩ đây là rắn 'Rhabdophis subminiatus' (Hoa cỏ cổ đỏ).", MediaUrl = "" },
                                    new FirstAidStep { Text = "Theo dõi sát màu nước tiểu và tình trạng chảy máu.", MediaUrl = "" }
                                },
                                Donts = new List<FirstAidStep> {
                                    new FirstAidStep { Text = "KHÔNG ĐƯỢC CHỦ QUAN nếu thấy vết cắn không sưng đau nhiều lúc đầu.", MediaUrl = "" },
                                    new FirstAidStep { Text = "KHÔNG dùng ga-rô chặt (làm tăng hoại tử và rối loạn đông máu tại chỗ).", MediaUrl = "" },
                                    new FirstAidStep { Text = "KHÔNG rạch hoặc hút máu tại vết cắn.", MediaUrl = "" }
                                },
                                Notes = new List<string> {
                                    "Lưu ý quan trọng: Việt Nam chưa có huyết thanh kháng độc cho loài này. Việc điều trị chủ yếu là hỗ trợ, truyền máu và lọc thận.",
                                    "Loài này có răng độc nằm sâu phía sau hàm (Hậu nha), nọc độc chỉ tiết ra khi rắn nhai hoặc cắn sâu."
                                }
                            }
                        }
                    },

                    new SnakeSpecies
                    {
                        Id = 9,
                        ScientificName = "Coelognathus radiatus",
                        CommonName = "Rắn Hổ Ngựa",
                        Slug = "ran-ho-ngua",
                        Description = "Loài rắn không độc, di chuyển cực nhanh và thường có hành vi tự vệ hung dữ.",
                        IdentificationSummary = "Màu nâu vàng, có 4 sọc đen chạy dọc phần trước thân, đầu dài.",
                        PrimaryVenomType = PrimaryVenomType.None,
                        RiskLevel = 1.0f,
                        IsVenomous = false,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/122/1737364008_0.jpg",
                        Identification = new IdentificationFeature
                        {
                            PhysicalTraits = new List<string> { "Có thể dài đến 2m", "4 sọc đen trên thân trước", "Đầu dài bầu dục", "Mắt lớn" },
                            Behaviors = new List<string> { "Ngóc cao thân mình, bẹt cổ để hù dọa giống rắn hổ", " Miệng há rộng, hung hăng, doạ nạt, dữ tợn khi bị đe dọa", "Giả chết nếu cảm thấy nguy hiểm trước đối phương" },
                            Habitat = "Đồng ruộng, bụi cây, khu vực nông nghiệp"
                        },
                        SymptomsByTime = new List<SymptomTimeline>
                        {
                            new SymptomTimeline { TimeRange = "Sau khi cắn", Signs = new List<string> { "Vết xước li ti", "Chảy máu nhẹ", "Không có triệu chứng thần kinh hay sưng nề lớn" }, IsCritical = false }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride
                        {
                            Mode = OverrideMode.Append,
                            Content = new FirstAidContent
                            {
                                Steps = new List<FirstAidStep>
                                {
                                    new FirstAidStep { Text = "Chỉ cần rửa sạch vết thương bằng xà phòng để tránh nhiễm trùng." }
                                }
                            }
                        }
                    },

                    new SnakeSpecies
                    {
                        Id = 10,
                        ScientificName = "Elaphe carinata",
                        CommonName = "Rắn Chuột Vua",
                        Slug = "ran-chuot-vua",
                        Description = "Loài rắn không độc nhưng cực kỳ hung dữ, có kích thước lớn và mùi hôi đặc trưng để xua đuổi kẻ thù. Dễ bị nhầm lẫn với rắn hổ mang chúa do kích thước lớn và họa tiết đầu gần giống nhau.",
                        IdentificationSummary = "Thân màu nâu vàng hoặc xám đen với các vảy có gờ nổi rất mạnh (nhám). Không có nanh độc, không phình mang.",
                        PrimaryVenomType = PrimaryVenomType.None,
                        RiskLevel = 2.0f,
                        IsVenomous = false,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/123/1737365069_2.jpeg",
                        Identification = new IdentificationFeature
                        {
                            PhysicalTraits = new List<string> { "Vảy có gờ nổi rất rõ (thân nhám)", "Mắt lớn, đầu thuôn dài", "Kích thước có thể lên tới 2.4m", "Màu sắc pha trộn vàng - đen - nâu ô liu" },
                            Behaviors = new List<string> { "Cực kỳ hung dữ, sẵn sàng tấn công khi bị kích động", "Phát ra mùi hôi thối nồng nặc từ tuyến sau", "Ăn thịt các loài rắn khác (kể cả rắn độc)" },
                            Habitat = "Vùng đồi núi, bụi rậm, trang trại chăn nuôi"
                        },
                        SymptomsByTime = new List<SymptomTimeline>
                        {
                            new SymptomTimeline { TimeRange = "Sau khi cắn", Signs = new List<string> { "Vết cắn hình vòng cung", "Chảy máu khá nhiều do răng sắc nhọn", "Đau rát cục bộ" }, IsCritical = false }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride
                        {
                            Mode = OverrideMode.Append,
                            Content = new FirstAidContent
                            {
                                Steps = new List<FirstAidStep>
                                {
                                    new FirstAidStep { Text = "Sát trùng kỹ vết thương vì miệng loài này chứa nhiều vi khuẩn do ăn chuột và thịt thối." }
                                }
                            }
                        }
                    },

                    // 11. RẮN LỤC CƯỜM (LỤC GẤM) - Protobothrops mucrosquamatus
                    new SnakeSpecies
                    {
                        Id = 11,
                        ScientificName = "Protobothrops mucrosquamatus",
                        CommonName = "Rắn Lục Cườm",
                        Slug = "ran-luc-cuom",
                        Description = "Loài rắn độc máu nguy hiểm, đầu hình tam giác lớn, hoa văn đốm sâm so le nhau ở sống lưng.",
                        IdentificationSummary = "Đầu tam giác rõ rệt, thân có các vệt hoa văn màu nâu đen trên nền xám/vàng đất, vảy nhám.",
                        PrimaryVenomType = PrimaryVenomType.Hemotoxic,
                        RiskLevel = 8.5f,
                        IsVenomous = true,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/54/1736524361_0.jpg",
                        Identification = new IdentificationFeature {
                            PhysicalTraits = new List<string> { "Đầu tam giác lớn", "Hoa văn vện gấm đốm nâu", "Vảy nhám", "Mắt có con ngươi dọc" },
                            Behaviors = new List<string> { "Hoạt động đêm",", Chuyên phục kích săn mồi", "Tính hung dữ, sẵn sàng tấn công", "Thường ở hốc đá, bụi rậm, lá khô" },
                            Habitat = "Rừng núi, vùng đồi gò, hang hốc"
                        },
                        SymptomsByTime = new List<SymptomTimeline> {
                            new SymptomTimeline { TimeRange = "0 - 15 phút", Signs = new List<string> { "Đau rát dữ dội", "Sưng nề tức thì" }, IsCritical = false },
                            new SymptomTimeline { TimeRange = "1 - 6 giờ", Signs = new List<string> { "Xuất huyết dưới da", "Máu chảy không cầm tại vết cắn", "Bầm tím nặng" }, IsCritical = true }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride {
                            Mode = OverrideMode.Replace, Content = new FirstAidContent { Steps = new List<FirstAidStep> { new FirstAidStep { Text = "KHÔNG garô/băng ép." }, new FirstAidStep { Text = "Bất động chi bằng nẹp lỏng." }, new FirstAidStep { Text = "Chuyển viện gấp." } } } }
                    },

                    // 12. RẮN LỤC NƯA (CHÀM QUẠP) - Calloselasma rhodostoma
                    new SnakeSpecies
                    {
                        Id = 12,
                        ScientificName = "Calloselasma rhodostoma",
                        CommonName = "Rắn Lục Nưa",
                        Slug = "ran-luc-nua",
                        Description = "Loài rắn cực nguy hiểm ở miền Nam/Tây Nguyên, ngụy trang hoàn hảo dưới lá khô.",
                        IdentificationSummary = "Thân mập, đầu tam giác, hoa văn hình tam giác sẫm màu dọc hai bên thân.",
                        PrimaryVenomType = PrimaryVenomType.Hemotoxic, // và có độc tế bào cytotoxic
                        RiskLevel = 9.5f,
                        IsVenomous = true,
                        ImageUrl = "https://cdn.kienthuc.net.vn/images/cf739f51f3276a5be16e9cbb75eb670590e4e1a049c04ce64210426ce976f3c08b805acba731385fd614eebaf46f4c3502d128915c73af35a8698059b85f0f9824f61d459aaa6ca7ad4acb289d18b91958ebfab71a3d1d5ad62d6dd95e9bd598a65c32335617c1b43812b9de6f4caea5/thot-tim-loai-ran-cuc-doc-nam-im-lim-cho-can-nguoi-o-viet-nam-Hinh-9.png",
                        Identification = new IdentificationFeature {
                            PhysicalTraits = new List<string> { "Thân mập, ngắn", "Hoa văn hình tam giác đối xứng", "Màu nâu lá khô", "Đầu tam giác rất nhọn" },
                            Behaviors = new List<string> { "Nằm bất động dưới lá khô", "Không bỏ chạy khi có người, chủ động cắn", "Tấn công bất ngờ cực nhanh" },
                            Habitat = "Rừng cao su, vườn điều, rừng khộp"
                        },
                        SymptomsByTime = new List<SymptomTimeline> {
                            new SymptomTimeline { TimeRange = "0 - 30 phút", Signs = new List<string> { "Sưng nề cực nhanh", "Đau buốt như lửa đốt" }, IsCritical = true },
                            new SymptomTimeline { TimeRange = "6 - 12 giờ", Signs = new List<string> { "Hoại tử mô diện rộng", "Xuất huyết toàn thân", "Phồng rộp máu" }, IsCritical = true }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride { Mode = OverrideMode.Replace, Content = new FirstAidContent { Steps = new List<FirstAidStep> { new FirstAidStep { Text = "Tuyệt đối không rạch vết thương vì nọc gây rối loạn đông máu cực nặng." }, new FirstAidStep { Text = "Băng ép nhẹ bằng băng thun (không chặt)." } } } }
                    },

                    // 13. RẮN LỤC XANH - Trimeresurus stejnegeri
                    new SnakeSpecies
                    {
                        Id = 13,
                        ScientificName = "Trimeresurus stejnegeri",
                        CommonName = "Rắn Lục Xanh (Lục Vẻ)",
                        Slug = "ran-luc-xanh",
                        Description = "Thường bị nhầm với lục đuôi đỏ nhưng không có màu đỏ ở đuôi, độc tính tương tự.",
                        IdentificationSummary = "Toàn thân xanh lá, đầu tam giác, có hố nhiệt giữa mắt và mũi.",
                        PrimaryVenomType = PrimaryVenomType.Hemotoxic,
                        RiskLevel = 7.0f,
                        IsVenomous = true,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/34/1736396208_0.jpg",
                        Identification = new IdentificationFeature {
                            PhysicalTraits = new List<string> { "Thân xanh mướt", "Đầu tam giác", "Mắt vàng/cam", "Không có đuôi đỏ" },
                            Behaviors = new List<string> { "Sống hoàn toàn trên cây", "Ngụy trang trong lá", "Hoạt động ban đêm" },
                            Habitat = "Bụi rậm, vườn cây trái, rừng rậm"
                        },
                        SymptomsByTime = new List<SymptomTimeline> {
                            new SymptomTimeline { TimeRange = "0 - 15 phút", Signs = new List<string> { "Sưng đau cục bộ", "Buồn nôn" }, IsCritical = false },
                            new SymptomTimeline { TimeRange = "2 - 12 giờ", Signs = new List<string> { "Vết thương thâm đen", "Chảy máu chân răng" }, IsCritical = true }
                        }
                    },

                    // 14. RẮN KHIẾM VẠCH - Oligodon fasciolatus
                    new SnakeSpecies
                    {
                        Id = 14,
                        ScientificName = "Oligodon fasciolatus",
                        CommonName = "Rắn Khiếm Vạch",
                        Slug = "ran-khiem-vach",
                        Description = "Loài rắn không độc nhưng có răng sắc nhọn để ăn trứng chim/bò sát.",
                        IdentificationSummary = "Kích thước nhỏ, tối đa 45 cm. Màu nâu/xám, có các vạch ngang mờ và hình chữ V trên đỉnh đầu.",
                        PrimaryVenomType = PrimaryVenomType.None,
                        RiskLevel = 1.5f,
                        IsVenomous = false,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/154/1737700148_0.jpg",
                        Identification = new IdentificationFeature {
                            PhysicalTraits = new List<string> { "Hình chữ V trên đầu", "Vảy trơn bóng", "Đầu bầu dục", "Kích thước nhỏ (tối đa 45 cm)", "Có 2 sọc đen dọc thân" },
                            Behaviors = new List<string> { "Săn mồi ban ngày", "Khá nhút nhát", "Thường gặp dưới đống gạch đá" },
                            Habitat = "Vườn nhà, khu vực nông nghiệp, rừng thưa"
                        },
                        SymptomsByTime = new List<SymptomTimeline> {
                            new SymptomTimeline { TimeRange = "Sau khi cắn", Signs = new List<string> { "Vết thương lớn", "Chảy máu nhiều", "Không sưng nề" }, IsCritical = false }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride
                        {
                            Mode = OverrideMode.Replace,
                            Content = new FirstAidContent
                            {
                                Steps = new List<FirstAidStep>
                                {
                                    new FirstAidStep { Text = "Rửa sạch vết thương bằng xà phòng hoặc dung dịch sát khuẩn." },
                                    new FirstAidStep { Text = "Cầm máu nếu cần thiết." },
                                    new FirstAidStep { Text = "Theo dõi dấu hiệu nhiễm trùng (sưng, đỏ, mưng mủ)." },
                                    new FirstAidStep { Text = "Đến cơ sở y tế nếu vết thương không lành hoặc có dấu hiệu nhiễm trùng." }
                                }
                            }
                        }
                    },

                    // 15. RẮN CƯỜM (HẢI XÀ) - Chrysopelea ornata
                    new SnakeSpecies
                    {
                        Id = 15,
                        ScientificName = "Chrysopelea ornata",
                        CommonName = "Rắn Cườm (Rắn Bay)",
                        Slug = "ran-cuom",
                        Description = "Loài rắn nước có khả năng 'bay' bằng cách hóp bụng để lượn qua các cành cây.",
                        IdentificationSummary = "Màu xanh vàng nhạt với các họa tiết viền đen chi tiết trên từng vảy.",
                        PrimaryVenomType = PrimaryVenomType.None,
                        RiskLevel = 1.0f,
                        IsVenomous = false,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/3/1737361056_0.jpg",
                        Identification = new IdentificationFeature {
                            PhysicalTraits = new List<string> { "Vảy màu vàng chanh viền đen", "Thân thon dài", "Mắt to tròn" },
                            Behaviors = new List<string> { "Leo trèo cực giỏi", "Nhảy từ trên cây cao xuống", "Rất hiền lành" },
                            Habitat = "Cây cao, vườn nhà, rừng rậm"
                        }
                    },

                    // 16. RẮN RÁO TRÂU - Ptyas mucosa
                    new SnakeSpecies
                    {
                        Id = 16,
                        ScientificName = "Ptyas mucosa",
                        CommonName = "Rắn Ráo Trâu (Rắn Lãi Lớn)",
                        Slug = "ran-rao-trau",
                        Description = "Loài rắn không độc có kích thước lớn, di chuyển tốc độ rất nhanh.",
                        IdentificationSummary = "Màu nâu/vàng đất, nửa thân sau có các vạch đen ngang rõ rệt như vằn hổ. Mắt rất to. Họa tiet đầu giống rắn hổ mang.",
                        PrimaryVenomType = PrimaryVenomType.None,
                        RiskLevel = 2.0f,
                        IsVenomous = false,
                        ImageUrl = "https://sgaqua.vn/wp-content/uploads/2026/01/ran-rao-trau-co-doc-khong-1.jpg",
                        Identification = new IdentificationFeature {
                            PhysicalTraits = new List<string> { "Kích thước lớn (tới 3m)", "Vằn ngang zig zag trắng nửa thân trước chuyển đen nửa thân sau", "Mắt rất to, tròn", "Vảy trơn, óng ánh, xếp đều", "Họa tiết vảy đầu giống rắn hổ mang" },
                            Behaviors = new List<string> { "Chạy trốn cực nhanh", "Hung dữ khi bị dồn vào đường cùng", "Bị đe dọa sẽ mở rộng vùng cổ và tạo âm thanh rít liên tục" },
                            Habitat = "Đồng ruộng, bụi rậm, hang hốc"
                        }
                    },

                    // 17. RẮN HOA CÂN VÂN ĐỐM - Sinonatrix aequifasciata
                    new SnakeSpecies
                    {
                        Id = 17,
                        ScientificName = "Sinonatrix aequifasciata",
                        CommonName = "Rắn Hoa Cân Vân Đốm",
                        Slug = "ran-hoa-can-van-dom",
                        Description = "Rắn nước không độc, thường sống gần các khe suối.",
                        IdentificationSummary = "Kích thước trung bình từ 0.7-1.4m. Thân mập, có các hoa văn hình mắt màu vàng đen chạy dọc thân.",
                        PrimaryVenomType = PrimaryVenomType.None,
                        RiskLevel = 1.0f,
                        IsVenomous = false,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/171/trimerodytes-aequifasciatus_1740063874_0.jpg",
                        Identification = new IdentificationFeature {
                            PhysicalTraits = new List<string> { "Thân mập hình trụ", "Hoa văn hình mắt màu vàng đen chạy dọc thân", "Đầu bầu dục" },
                            Behaviors = new List<string> { "Sống bán thủy sinh", "Ăn cá và ếch nhái" },
                            Habitat = "Suối, ao hồ, đầm lầy vùng núi"
                        }
                    },

                    // 18. RẮN RI CÁ - Homalopsis buccata
                    new SnakeSpecies
                    {
                        Id = 18,
                        ScientificName = "Homalopsis buccata",
                        CommonName = "Rắn Ri Cá",
                        Slug = "ran-ri-ca",
                        Description = "Rắn nước phổ biến ở Nam Bộ, thịt ngon nhưng không có độc.",
                        IdentificationSummary = "Kích thước trung bình khoảng 70cm.Đầu to, có hình mặt nạ trắng trên đầu, thân có nhiều khoanh màu nâu đỏ nhạt.",
                        PrimaryVenomType = PrimaryVenomType.None,
                        RiskLevel = 1.0f,
                        IsVenomous = false,
                        ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/c/c1/Homalopsis_buccata.png",
                        Identification = new IdentificationFeature {
                            PhysicalTraits = new List<string> { "Kích thước trung bình khoảng 70cm", "Đầu to rộng", "Hoa văn mặt nạ trên đỉnh đầu", "Thân chắc, vảy gồ" },
                            Behaviors = new List<string> { "Ăn đêm", "Sống dưới nước", "Nhút nhát" },
                            Habitat = "Kênh rạch, ao hồ, đầm lầy bùn"
                        }
                    },

                    // 19. RẮN ROI - Ahaetulla prasina
                    new SnakeSpecies
                    {
                        Id = 19,
                        ScientificName = "Ahaetulla prasina",
                        CommonName = "Rắn Roi (Rắn Sinh Viên)",
                        Slug = "ran-roi",
                        Description = "Thân mảnh như sợi dây thừng, đầu rất nhọn, độc nhẹ, không gây nguy hiểm cho người.",
                        IdentificationSummary = "Màu xanh lá huỳnh quang nổi bật, mõm dài và nhọn, con ngươi nằm ngang.",
                        PrimaryVenomType = PrimaryVenomType.None,
                        RiskLevel = 1.0f,
                        IsVenomous = false,
                        ImageUrl = "https://cdn-i.vtcnews.vn/files/f2/2015/01/21/nhung-loai-ran-ky-di-nhat-the-gioi-tai-viet-nam-0.jpg",
                        Identification = new IdentificationFeature {
                            PhysicalTraits = new List<string> { "Thân cực mảnh", "Mõm nhọn dài", "Con ngươi ngang đặc trưng", "Màu xanh lá hoặc nâu nhạt" },
                            Behaviors = new List<string> { "Sống trên cây", "Di chuyển chậm chạp", "Hay thò thụt lưỡi đánh hơi" },
                            Habitat = "Vườn cây, rừng thưa, bụi rậm"
                        }
                    },

                    // 20. RẮN TRUN - Cylindrophis ruffus
                    new SnakeSpecies
                    {
                        Id = 20,
                        ScientificName = "Cylindrophis ruffus",
                        CommonName = "Rắn Trun",
                        Slug = "ran-trun",
                        Description = "Loài rắn không độc, thân hình trụ tròn, thường bị nhầm với rắn độc do màu sắc.",
                        IdentificationSummary = "Thân đen bóng có vạch vàng/trắng, đuôi ngắn tịt và có màu đỏ dưới mặt đuôi.",
                        PrimaryVenomType = PrimaryVenomType.None,
                        RiskLevel = 1.0f,
                        IsVenomous = false,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/131/1737365540_0.jpg",
                        Identification = new IdentificationFeature {
                            PhysicalTraits = new List<string> { "Thân hình trụ đồng nhất", "Đuôi ngắn giống đầu", "Mặt dưới đuôi màu đỏ" },
                            Behaviors = new List<string> { "Chui rúc trong bùn đất", "Khi gặp nguy hiểm sẽ cuộn tròn và giơ đuôi đỏ lên để lừa kẻ thù" },
                            Habitat = "Đầm lầy, ruộng lúa, nơi đất ẩm"
                        }
                    },

                    // 22. RẮN ĐAI LỚN - Lycodon fasciatus
                    new SnakeSpecies
                    {
                        Id = 21,
                        ScientificName = "Ptyas major", // Tên khoa học chính xác của Rắn Đại Lớn
                        CommonName = "Rắn Đai Lớn (Rắn Xanh Lớn)",
                        Slug = "ran-dai-lon-xanh",
                        Description = "Loài rắn hoàn toàn không độc, hiền lành, thường bị nhầm với rắn lục do màu xanh lục toàn thân.",
                        IdentificationSummary = "Kích thước có thể đạt 1,5-2,5m. Không độc, toàn thân màu xanh lá mượt mà, mắt rất to và tròn, đuôi thuôn dài.",
                        PrimaryVenomType = PrimaryVenomType.None,
                        RiskLevel = 1.0f,
                        IsVenomous = false,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/101/ptyas-major_1743324730_0.jpg",
                        Identification = new IdentificationFeature {
                            PhysicalTraits = new List<string> {
                                "Thân dài, có thể đạt 1,5-2,5 mét",
                                "Toàn thân màu xanh lá cây đồng nhất",
                                "Bụng màu vàng nhạt hoặc trắng xanh",
                                "Mắt rất to, con ngươi tròn đen",
                                "Vảy trơn mịn, bóng mượt"
                            },
                            Behaviors = new List<string> {
                                "Hoạt động chủ yếu ban ngày",
                                "Rất hiền lành, hiếm khi cắn người kể cả khi bị bắt",
                                "Di chuyển nhanh nhẹn trên mặt đất và cây cỏ"
                            },
                            Habitat = "Vườn tược, bụi rậm, rừng thưa, thường gặp ở vùng đồi núi và trung du"
                        },
                        SymptomsByTime = new List<SymptomTimeline> {
                            new SymptomTimeline {
                                TimeRange = "Sau khi cắn",
                                Signs = new List<string> { "Vết xước rất nhỏ", "Hầu như không đau", "Không sưng nề" },
                                IsCritical = false
                            }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride {
                            Mode = OverrideMode.Replace,
                            Content = new FirstAidContent {
                                Steps = new List<FirstAidStep> {
                                    new FirstAidStep { Text = "Rửa sạch vết thương bằng nước hoặc xà phòng." },
                                    new FirstAidStep { Text = "Bình tĩnh vì đây là loài rắn ích lợi, chuyên ăn côn trùng và sâu bọ." }
                                }
                            }
                        }
                    },
                    new SnakeSpecies
                    {
                        Id = 22,
                        ScientificName = "Amphiesma stolatum",
                        CommonName = "Rắn Sãi Cỏ",
                        Slug = "ran-sai-co",
                        Description = "Loài rắn nước không độc, hiền lành và có ích cho nông nghiệp. Chúng thường bị nhầm lẫn với một số loài rắn khác do hoa văn phức tạp.",
                        IdentificationSummary = "Không độc, dài trung bình 40cm - 80cm. Thân có 2 sọc sáng màu song song, nối với nhau bởi các vạch ngang tối màu trông như chiếc thang.",
                        PrimaryVenomType = PrimaryVenomType.None,
                        RiskLevel = 1.0f,
                        IsVenomous = false,
                        ImageUrl = "https://vietnamsnakes.com/storage/snakes/species/27/1736340349_0.jpg",
                        Identification = new IdentificationFeature {
                            PhysicalTraits = new List<string> {
                                "Chiều dài trung bình 40 - 80 cm",
                                "2 sọc sáng màu chạy dọc song song trên lưng",
                                "Các vạch ngang tối màu nối 2 sọc giống hình chiếc thang",
                                "Bụng màu kem nhạt với đốm đen nhỏ hai bên thân",
                                "Mép miệng màu vàng nhạt với vạch đen trước và sau mắt"
                            },
                            Behaviors = new List<string> {
                                "Hoạt động chủ yếu vào ban ngày (nhật hành)",
                                "Tính tình nhút nhát, thường bỏ chạy khi gặp người",
                                "Săn các sinh vật nhỏ như cá, giun đất và tắc kè"
                            },
                            Habitat = "Vùng đồng bằng và đồi núi, thường ở gần nguồn nước (ao, hồ, suối)"
                        },
                        SymptomsByTime = new List<SymptomTimeline> {
                            new SymptomTimeline {
                                TimeRange = "Sau khi cắn",
                                Signs = new List<string> { "Vết xước nhỏ hình vòng cung", "Chảy máu nhẹ", "Không sưng nề, không gây độc" },
                                IsCritical = false
                            }
                        },
                        FirstAidGuidelineOverride = new FirstAidOverride {
                            Mode = OverrideMode.Replace,
                            Content = new FirstAidContent {
                                Steps = new List<FirstAidStep> {
                                    new FirstAidStep { Text = "Rửa vết thương bằng xà phòng và nước sạch để tránh nhiễm trùng." },
                                    new FirstAidStep { Text = "Bình tĩnh vì đây là loài rắn hoàn toàn vô hại." }
                                }
                            }
                        }
                    }
                };
                context.SnakeSpecies.AddRange(snakes);
                await context.SaveChangesAsync();
            }

            // ==================================================================================
            // SEED SPECIES VENOMS
            // ==================================================================================
            // Depends on: SnakeSpecies, VenomTypes
            if (!context.SpeciesVenoms.Any())
            {
                var speciesVenoms = new List<SpeciesVenom>
                {
                    // Rắn Cạp Nia Bắc (Độc thần kinh)
                    new SpeciesVenom { SnakeSpeciesId = 1, VenomTypeId = 1 },
                    // Rắn Lục Đuôi Đỏ (Độc máu)
                    new SpeciesVenom { SnakeSpeciesId = 2, VenomTypeId = 2 },
                    // Rắn Hổ Mang Chúa (Độc thần kinh)
                    new SpeciesVenom { SnakeSpeciesId = 3, VenomTypeId = 1 },
                    new SpeciesVenom { SnakeSpeciesId = 3, VenomTypeId = 3 }, // Hỗn hợp: Thần kinh + Tế bào
                    // Rắn Cạp Nong (Độc thần kinh)
                    new SpeciesVenom { SnakeSpeciesId = 5, VenomTypeId = 1 },
                    // Rắn Cạp Nia Nam (Độc thần kinh)
                    new SpeciesVenom { SnakeSpeciesId = 6, VenomTypeId = 1 },
                    // Rắn Hổ Mang Xiêm (Hỗn hợp: Thần kinh + Tế bào)
                    new SpeciesVenom { SnakeSpeciesId = 7, VenomTypeId = 1 },
                    new SpeciesVenom { SnakeSpeciesId = 7, VenomTypeId = 3 },
                    // Rắn Hoa Cỏ Cổ Đỏ (Độc máu)
                    new SpeciesVenom { SnakeSpeciesId = 8, VenomTypeId = 2 },
                    // Rắn Lục Cườm (Độc máu)
                    new SpeciesVenom { SnakeSpeciesId = 11, VenomTypeId = 2 },
                    // Rắn Lục Nưa / Chàm Quạp (Hỗn hợp: Máu + Tế bào)
                    new SpeciesVenom { SnakeSpeciesId = 12, VenomTypeId = 2 },
                    new SpeciesVenom { SnakeSpeciesId = 12, VenomTypeId = 3 },
                    // Rắn Lục Xanh (Độc máu)
                    new SpeciesVenom { SnakeSpeciesId = 13, VenomTypeId = 2 }
                };
                context.SpeciesVenoms.AddRange(speciesVenoms);
                await context.SaveChangesAsync();
            }

            // ==================================================================================
            // SEED SNAKE SPECIES NAMES (Alternative names)
            // ==================================================================================
            // Depends on: SnakeSpecies
            if (!context.SnakeSpeciesNames.Any())
            {
                var speciesNames = new List<SnakeSpeciesName>
                {
                    // 1. Rắn Cạp Nia Bắc (ID: 1)
                    new SnakeSpeciesName { Name = "Rắn Cạp Nia", Slug = "ran-cap-nia", SnakeSpeciesId = 1 },
                    new SnakeSpeciesName { Name = "Rắn Nia Bắc", Slug = "ran-nia-bac", SnakeSpeciesId = 1 },
                    new SnakeSpeciesName { Name = "Rắn Nia Khoanh Trắng", Slug = "ran-nia-khoanh-trang", SnakeSpeciesId = 1 },

                    // 2. Rắn Lục Đuôi Đỏ (ID: 2)
                    new SnakeSpeciesName { Name = "Rắn Lục Tre", Slug = "ran-luc-tre", SnakeSpeciesId = 2 },
                    new SnakeSpeciesName { Name = "Rắn Lục Xanh Đuôi Đỏ", Slug = "ran-luc-xanh-duoi-do", SnakeSpeciesId = 2 },

                    // 3. Rắn Hổ Mang Chúa (ID: 3)
                    new SnakeSpeciesName { Name = "Rắn Hổ Mây", Slug = "ran-ho-may", SnakeSpeciesId = 3 },
                    new SnakeSpeciesName { Name = "Rắn Hổ Chúa", Slug = "ran-ho-chua", SnakeSpeciesId = 3 },
                    new SnakeSpeciesName { Name = "Rắn Hổ Mang Lớn", Slug = "ran-ho-mang-lon", SnakeSpeciesId = 3 },

                    // 5. Rắn Cạp Nong (ID: 5)
                    new SnakeSpeciesName { Name = "Rắn Mai Gầm", Slug = "ran-mai-gam", SnakeSpeciesId = 5 },
                    new SnakeSpeciesName { Name = "Rắn Nia Vàng", Slug = "ran-nia-vang", SnakeSpeciesId = 5 },

                    // 6. Rắn Cạp Nia Nam (ID: 6)
                    new SnakeSpeciesName { Name = "Rắn Nia Khoanh Đều", Slug = "ran-nia-khoanh-deu", SnakeSpeciesId = 6 },
                    new SnakeSpeciesName { Name = "Rắn Vòng Bạc", Slug = "ran-vong-bac", SnakeSpeciesId = 6 },
                    new SnakeSpeciesName { Name = "Rắn Mai Bạc", Slug = "ran-mai-bac", SnakeSpeciesId = 6 },
                    new SnakeSpeciesName { Name = "Rắn Nia Nam", Slug = "ran-nia-nam", SnakeSpeciesId = 6 },

                    // 7. Rắn Mang Xiêm (ID: 7)
                    new SnakeSpeciesName { Name = "Rắn Hổ Bành", Slug = "ran-ho-banh", SnakeSpeciesId = 7 },
                    new SnakeSpeciesName { Name = "Rắn Hổ Mèo", Slug = "ran-ho-meo", SnakeSpeciesId = 7 },

                    // 8. Rắn Hoa Cỏ Cổ Đỏ (ID: 8)
                    new SnakeSpeciesName { Name = "Rắn Học Trò", Slug = "ran-hoc-tro", SnakeSpeciesId = 8 },
                    new SnakeSpeciesName { Name = "Rắn Hổ Lửa", Slug = "ran-ho-lua", SnakeSpeciesId = 8 },
                    new SnakeSpeciesName { Name = "Rắn Nữ Hoàng Bóng Đêm", Slug = "ran-nu-hoang-bong-dem", SnakeSpeciesId = 8 },
                    

                    // 9. Rắn Hổ Ngựa (ID: 9)
                    new SnakeSpeciesName { Name = "Rắn Sọc Dưa", Slug = "ran-soc-dua", SnakeSpeciesId = 9 },
                    new SnakeSpeciesName { Name = "Rắn Hổ Chó", Slug = "ran-ho-cho", SnakeSpeciesId = 9 },

                    // 10. Rắn Chuột Vua (ID: 10)
                    new SnakeSpeciesName { Name = "Rắn Sọc Gờ", Slug = "ran-soc-go", SnakeSpeciesId = 10 },

                    // 11. Rắn Lục Cườm (ID: 11)
                    new SnakeSpeciesName { Name = "Rắn Lục Habu", Slug = "ran-luc-habu", SnakeSpeciesId = 11 },

                    // 12. Rắn Lục Nưa (ID: 12)
                    new SnakeSpeciesName { Name = "Rắn Chàm Quạp", Slug = "ran-cham-quap", SnakeSpeciesId = 12 },
                    new SnakeSpeciesName { Name = "Rắn Hổ Bướm", Slug = "ran-ho-buom", SnakeSpeciesId = 12 },
                    new SnakeSpeciesName { Name = "Rắn Lá Khô", Slug = "ran-la-kho", SnakeSpeciesId = 12 },
                    new SnakeSpeciesName { Name = "Rắn Cà Tênh", Slug = "ran-ca-tenh", SnakeSpeciesId = 12 },

                    // 13. Rắn Lục Xanh (ID: 13)
                    new SnakeSpeciesName { Name = "Rắn Lục", Slug = "ran-luc", SnakeSpeciesId = 13 },

                    // 16. Rắn Ráo Trâu (ID: 16)
                    new SnakeSpeciesName { Name = "Rắn Hổ Trâu", Slug = "ran-ho-trau", SnakeSpeciesId = 16 },
                    new SnakeSpeciesName { Name = "Rắn Hổ Hèo", Slug = "ran-ho-heo", SnakeSpeciesId = 16 },
                    new SnakeSpeciesName { Name = "Rắn Long Thừa", Slug = "ran-long-thua", SnakeSpeciesId = 16 },
                    new SnakeSpeciesName { Name = "Rắn Hổ Vện", Slug = "ran-ho-ven", SnakeSpeciesId = 16 },

                    // 19. Rắn Roi (ID: 19)
                    new SnakeSpeciesName { Name = "Rắn Lục Kim", Slug = "ran-luc-kim", SnakeSpeciesId = 19 },
                    new SnakeSpeciesName { Name = "Rắn Lá Cây", Slug = "ran-la-cay", SnakeSpeciesId = 19 },

                    // 22. Rắn Sãi Cỏ (ID: 22)
                    new SnakeSpeciesName { Name = "Rắn Sãi Thường", Slug = "ran-sai-thuong", SnakeSpeciesId = 22 },
                };
                context.SnakeSpeciesNames.AddRange(speciesNames);
                await context.SaveChangesAsync();
            }

            // ==================================================================================
            // SEED FILTER SNAKE MAPPINGS
            // ==================================================================================
            // Depends on: SnakeSpecies, FilterOptions
            if (!context.FilterSnakeMappings.Any())
            {
                var mappings = new List<FilterSnakeMapping>
                {
                    // --- 1. RẮN CẠP NIA BẮC (SnakeId: 1) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 1, FilterOptionId = 1 }, // Miền Bắc
                    new FilterSnakeMapping { SnakeSpeciesId = 1, FilterOptionId = 4 }, // Trong nhà
                    new FilterSnakeMapping { SnakeSpeciesId = 1, FilterOptionId = 5 }, // Dưới nước (ao hồ)
                    new FilterSnakeMapping { SnakeSpeciesId = 1, FilterOptionId = 7 }, // Đồng ruộng
                    new FilterSnakeMapping { SnakeSpeciesId = 1, FilterOptionId = 10 }, // Đầu bầu dục
                    new FilterSnakeMapping { SnakeSpeciesId = 1, FilterOptionId = 12 }, // Đen
                    new FilterSnakeMapping { SnakeSpeciesId = 1, FilterOptionId = 16 }, // Khoanh tròn

                    // --- 2. RẮN LỤC ĐUÔI ĐỎ (SnakeId: 2) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 2, FilterOptionId = 1 },
                    new FilterSnakeMapping { SnakeSpeciesId = 2, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 2, FilterOptionId = 3 },
                    new FilterSnakeMapping { SnakeSpeciesId = 2, FilterOptionId = 6 }, // Trên cây
                    new FilterSnakeMapping { SnakeSpeciesId = 2, FilterOptionId = 9 }, // Đầu tam giác
                    new FilterSnakeMapping { SnakeSpeciesId = 2, FilterOptionId = 11 }, // Xanh lá
                    new FilterSnakeMapping { SnakeSpeciesId = 2, FilterOptionId = 15 }, // Thân trơn
                    new FilterSnakeMapping { SnakeSpeciesId = 2, FilterOptionId = 20 }, // Đuôi đỏ
                    new FilterSnakeMapping { SnakeSpeciesId = 2, FilterOptionId = 22 }, // Vảy nhám

                    // --- 3. RẮN HỔ MANG CHÚA (SnakeId: 3) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 3, FilterOptionId = 1 },
                    new FilterSnakeMapping { SnakeSpeciesId = 3, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 3, FilterOptionId = 3 },
                    new FilterSnakeMapping { SnakeSpeciesId = 3, FilterOptionId = 8 }, // Hang hốc
                    new FilterSnakeMapping { SnakeSpeciesId = 3, FilterOptionId = 10 }, // Đầu bầu dục
                    new FilterSnakeMapping { SnakeSpeciesId = 3, FilterOptionId = 12 }, // Đen/Nâu
                    new FilterSnakeMapping { SnakeSpeciesId = 3, FilterOptionId = 18 }, // Vân phức tạp
                    new FilterSnakeMapping { SnakeSpeciesId = 3, FilterOptionId = 19 }, // Phình mang

                    // --- 4. RẮN RÁO (SnakeId: 4) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 4, FilterOptionId = 1 }, // Toàn quốc
                    new FilterSnakeMapping { SnakeSpeciesId = 4, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 4, FilterOptionId = 3 },
                    new FilterSnakeMapping { SnakeSpeciesId = 4, FilterOptionId = 7 }, // Đồng ruộng
                    new FilterSnakeMapping { SnakeSpeciesId = 4, FilterOptionId = 10 }, // Đầu bầu dục
                    new FilterSnakeMapping { SnakeSpeciesId = 4, FilterOptionId = 12 }, // Nâu đất
                    new FilterSnakeMapping { SnakeSpeciesId = 4, FilterOptionId = 15 }, // Thân trơn

                    // --- 5. RẮN CẠP NONG (SnakeId: 5) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 5, FilterOptionId = 1 }, // Toàn quốc
                    new FilterSnakeMapping { SnakeSpeciesId = 5, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 5, FilterOptionId = 3 },
                    new FilterSnakeMapping { SnakeSpeciesId = 5, FilterOptionId = 7 }, // Đồng ruộng
                    new FilterSnakeMapping { SnakeSpeciesId = 5, FilterOptionId = 8 }, // Hang hốc
                    new FilterSnakeMapping { SnakeSpeciesId = 5, FilterOptionId = 10 }, // Đầu bầu dục
                    new FilterSnakeMapping { SnakeSpeciesId = 5, FilterOptionId = 14 }, // Vàng/Cam
                    new FilterSnakeMapping { SnakeSpeciesId = 5, FilterOptionId = 16 }, // Khoanh tròn

                    // --- 6. RẮN CẠP NIA NAM (SnakeId: 6) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 6, FilterOptionId = 2 }, // Miền Trung/Nam
                    new FilterSnakeMapping { SnakeSpeciesId = 6, FilterOptionId = 3 },
                    new FilterSnakeMapping { SnakeSpeciesId = 6, FilterOptionId = 4 }, // Trong nhà
                    new FilterSnakeMapping { SnakeSpeciesId = 6, FilterOptionId = 7 }, // Đồng ruộng
                    new FilterSnakeMapping { SnakeSpeciesId = 6, FilterOptionId = 10 },
                    new FilterSnakeMapping { SnakeSpeciesId = 6, FilterOptionId = 12 }, // Đen
                    new FilterSnakeMapping { SnakeSpeciesId = 6, FilterOptionId = 16 }, // Khoanh tròn

                    // --- 7. RẮN HỔ MANG XIÊM (SnakeId: 7) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 7, FilterOptionId = 2 }, // Miền Trung
                    new FilterSnakeMapping { SnakeSpeciesId = 7, FilterOptionId = 3 }, // Miền Nam
                    new FilterSnakeMapping { SnakeSpeciesId = 7, FilterOptionId = 4 }, // Trong nhà
                    new FilterSnakeMapping { SnakeSpeciesId = 7, FilterOptionId = 7 }, // Đồng ruộng
                    new FilterSnakeMapping { SnakeSpeciesId = 7, FilterOptionId = 10 }, // Đầu bầu dục
                    new FilterSnakeMapping { SnakeSpeciesId = 7, FilterOptionId = 12 }, // Đen/Nâu
                    new FilterSnakeMapping { SnakeSpeciesId = 7, FilterOptionId = 19 }, // Phình mang

                    // --- 8. RẮN HOA CỎ CỔ ĐỎ (SnakeId: 8) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 8, FilterOptionId = 1 },
                    new FilterSnakeMapping { SnakeSpeciesId = 8, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 8, FilterOptionId = 3 },
                    new FilterSnakeMapping { SnakeSpeciesId = 8, FilterOptionId = 7 }, // Đồng ruộng
                    new FilterSnakeMapping { SnakeSpeciesId = 8, FilterOptionId = 11 }, // Xanh lá (ô liu)
                    new FilterSnakeMapping { SnakeSpeciesId = 8, FilterOptionId = 21 }, // Cổ đỏ/vàng

                    // --- 9. RẮN HỔ NGỰA / SỌC DƯA (SnakeId: 9) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 9, FilterOptionId = 1 }, // Toàn quốc
                    new FilterSnakeMapping { SnakeSpeciesId = 9, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 9, FilterOptionId = 3 },
                    new FilterSnakeMapping { SnakeSpeciesId = 9, FilterOptionId = 7 }, // Đồng ruộng
                    new FilterSnakeMapping { SnakeSpeciesId = 9, FilterOptionId = 10 },
                    new FilterSnakeMapping { SnakeSpeciesId = 9, FilterOptionId = 14 }, // Vàng nâu
                    new FilterSnakeMapping { SnakeSpeciesId = 9, FilterOptionId = 17 }, // Sọc dọc (4 sọc)

                    // --- 10. RẮN CHUỘT VUA / SỌC GỜ (SnakeId: 10) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 10, FilterOptionId = 1 }, // Chủ yếu miền Bắc
                    new FilterSnakeMapping { SnakeSpeciesId = 10, FilterOptionId = 8 }, // Hang hốc/Đá
                    new FilterSnakeMapping { SnakeSpeciesId = 10, FilterOptionId = 10 },
                    new FilterSnakeMapping { SnakeSpeciesId = 10, FilterOptionId = 12 }, // Nâu ô liu
                    new FilterSnakeMapping { SnakeSpeciesId = 10, FilterOptionId = 18 }, // Vân phức tạp
                    new FilterSnakeMapping { SnakeSpeciesId = 10, FilterOptionId = 22 }, // Vảy nhám

                    // --- 11. RẮN LỤC CƯỜM (SnakeId: 11) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 11, FilterOptionId = 1 }, // Bắc/Trung
                    new FilterSnakeMapping { SnakeSpeciesId = 11, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 11, FilterOptionId = 8 }, // Hang đá
                    new FilterSnakeMapping { SnakeSpeciesId = 11, FilterOptionId = 9 }, // Đầu tam giác
                    new FilterSnakeMapping { SnakeSpeciesId = 11, FilterOptionId = 13 }, // Xám đất
                    new FilterSnakeMapping { SnakeSpeciesId = 11, FilterOptionId = 18 }, // Vân phức tạp
                    new FilterSnakeMapping { SnakeSpeciesId = 11, FilterOptionId = 22 }, // Vảy nhám

                    // --- 12. RẮN LỤC NƯA / CHÀM QUẠP (SnakeId: 12) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 12, FilterOptionId = 2 }, // Miền Trung
                    new FilterSnakeMapping { SnakeSpeciesId = 12, FilterOptionId = 3 }, // Miền Nam
                    new FilterSnakeMapping { SnakeSpeciesId = 12, FilterOptionId = 8 }, // Dưới lá khô
                    new FilterSnakeMapping { SnakeSpeciesId = 12, FilterOptionId = 9 }, // Đầu tam giác
                    new FilterSnakeMapping { SnakeSpeciesId = 12, FilterOptionId = 13 }, // Xám/Màu đất
                    new FilterSnakeMapping { SnakeSpeciesId = 12, FilterOptionId = 18 }, // Vân phức tạp
                    new FilterSnakeMapping { SnakeSpeciesId = 12, FilterOptionId = 22 }, // Vảy nhám

                    // --- 13. RẮN LỤC XANH / LỤC VẺ (SnakeId: 13) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 13, FilterOptionId = 1 },
                    new FilterSnakeMapping { SnakeSpeciesId = 13, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 13, FilterOptionId = 6 }, // Trên cây
                    new FilterSnakeMapping { SnakeSpeciesId = 13, FilterOptionId = 9 }, // Đầu tam giác
                    new FilterSnakeMapping { SnakeSpeciesId = 13, FilterOptionId = 11 }, // Xanh lá
                    new FilterSnakeMapping { SnakeSpeciesId = 13, FilterOptionId = 15 }, // Thân trơn
                    new FilterSnakeMapping { SnakeSpeciesId = 13, FilterOptionId = 22 }, // Vảy nhám

                    // --- 14. RẮN KHIẾM VẠCH (SnakeId: 14) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 14, FilterOptionId = 1 },
                    new FilterSnakeMapping { SnakeSpeciesId = 14, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 14, FilterOptionId = 3 },
                    new FilterSnakeMapping { SnakeSpeciesId = 14, FilterOptionId = 8 }, // Hang đá/Gạch
                    new FilterSnakeMapping { SnakeSpeciesId = 14, FilterOptionId = 10 },
                    new FilterSnakeMapping { SnakeSpeciesId = 14, FilterOptionId = 13 }, // Xám/Nâu mờ
                    new FilterSnakeMapping { SnakeSpeciesId = 14, FilterOptionId = 18 }, // Vân phức tạp (chữ V đầu)

                    // --- 15. RẮN CƯỜM / RẮN BAY (SnakeId: 15) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 15, FilterOptionId = 1 },
                    new FilterSnakeMapping { SnakeSpeciesId = 15, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 15, FilterOptionId = 3 },
                    new FilterSnakeMapping { SnakeSpeciesId = 15, FilterOptionId = 6 }, // Trên cây
                    new FilterSnakeMapping { SnakeSpeciesId = 15, FilterOptionId = 10 },
                    new FilterSnakeMapping { SnakeSpeciesId = 15, FilterOptionId = 14 }, // Vàng chanh
                    new FilterSnakeMapping { SnakeSpeciesId = 15, FilterOptionId = 18 }, // Vân phức tạp

                    // --- 16. RẮN RÁO TRÂU / HỔ HÈO (SnakeId: 16) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 16, FilterOptionId = 1 },
                    new FilterSnakeMapping { SnakeSpeciesId = 16, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 16, FilterOptionId = 3 },
                    new FilterSnakeMapping { SnakeSpeciesId = 16, FilterOptionId = 7 }, // Đồng ruộng
                    new FilterSnakeMapping { SnakeSpeciesId = 16, FilterOptionId = 8 }, // Hang hốc
                    new FilterSnakeMapping { SnakeSpeciesId = 16, FilterOptionId = 10 },
                    new FilterSnakeMapping { SnakeSpeciesId = 16, FilterOptionId = 12 }, // Nâu/Đen
                    new FilterSnakeMapping { SnakeSpeciesId = 16, FilterOptionId = 18 }, // Vân phức tạp (vằn hổ)

                    // --- 17. RẮN HOA CÂN VÂN ĐỐM (SnakeId: 17) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 17, FilterOptionId = 1 },
                    new FilterSnakeMapping { SnakeSpeciesId = 17, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 17, FilterOptionId = 5 }, // Dưới nước
                    new FilterSnakeMapping { SnakeSpeciesId = 17, FilterOptionId = 10 },
                    new FilterSnakeMapping { SnakeSpeciesId = 17, FilterOptionId = 12 }, // Đen/Nâu
                    new FilterSnakeMapping { SnakeSpeciesId = 17, FilterOptionId = 18 }, // Vân phức tạp (hình mắt)

                    // --- 19. RẮN ROI (SnakeId: 19) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 19, FilterOptionId = 1 },
                    new FilterSnakeMapping { SnakeSpeciesId = 19, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 19, FilterOptionId = 3 },
                    new FilterSnakeMapping { SnakeSpeciesId = 19, FilterOptionId = 6 }, // Trên cây
                    new FilterSnakeMapping { SnakeSpeciesId = 19, FilterOptionId = 11 }, // Xanh lá
                    new FilterSnakeMapping { SnakeSpeciesId = 19, FilterOptionId = 15 }, // Thân trơn

                    // --- 20. RẮN TRUN (SnakeId: 20) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 20, FilterOptionId = 1 },
                    new FilterSnakeMapping { SnakeSpeciesId = 20, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 20, FilterOptionId = 3 },
                    new FilterSnakeMapping { SnakeSpeciesId = 20, FilterOptionId = 8 }, // Đất ẩm/Bùn
                    new FilterSnakeMapping { SnakeSpeciesId = 20, FilterOptionId = 10 },
                    new FilterSnakeMapping { SnakeSpeciesId = 20, FilterOptionId = 12 }, // Đen bóng
                    new FilterSnakeMapping { SnakeSpeciesId = 20, FilterOptionId = 16 }, // Khoanh vạch

                    // --- 21. RẮN ĐAI LỚN - Lycodon fasciatus (SnakeId: 21) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 21, FilterOptionId = 1 }, // Bắc/Trung
                    new FilterSnakeMapping { SnakeSpeciesId = 21, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 21, FilterOptionId = 5 },
                    new FilterSnakeMapping { SnakeSpeciesId = 21, FilterOptionId = 6 },
                    new FilterSnakeMapping { SnakeSpeciesId = 21, FilterOptionId = 7 },
                    new FilterSnakeMapping { SnakeSpeciesId = 21, FilterOptionId = 10 },
                    new FilterSnakeMapping { SnakeSpeciesId = 21, FilterOptionId = 11 }, // Xanh lá
                    new FilterSnakeMapping { SnakeSpeciesId = 21, FilterOptionId = 15 },  // Thân trơn màu xanh

                    // --- 22. RẮN SÃI CỎ (SnakeId: 22) ---
                    new FilterSnakeMapping { SnakeSpeciesId = 22, FilterOptionId = 1 },
                    new FilterSnakeMapping { SnakeSpeciesId = 22, FilterOptionId = 2 },
                    new FilterSnakeMapping { SnakeSpeciesId = 22, FilterOptionId = 3 },
                    new FilterSnakeMapping { SnakeSpeciesId = 22, FilterOptionId = 5 }, // Gần nước
                    new FilterSnakeMapping { SnakeSpeciesId = 22, FilterOptionId = 7 }, // Đồng ruộng
                    new FilterSnakeMapping { SnakeSpeciesId = 22, FilterOptionId = 13 }, // Xám/Đất
                    new FilterSnakeMapping { SnakeSpeciesId = 22, FilterOptionId = 17 }  // Sọc dọc
                };
                context.FilterSnakeMappings.AddRange(mappings);
                await context.SaveChangesAsync();
            }

            // ==================================================================================
            // SEED SYMPTOM CONFIGS
            // ==================================================================================
            // No direct dependency
            if (!context.SymptomConfigs.Any())
            {
                var symptomConfigs = new List<SymptomConfig>
                {
                    // ==================================================================================
                    // NHÓM 1: BACKGROUND - THÔNG TIN NẠN NHÂN (DisplayOrder: 1-3)
                    // ==================================================================================

                    // CÂU HỎI 1: Độ tuổi nạn nhân (DisplayOrder = 1)
                    new SymptomConfig
                    {
                        Id = 101,
                        GroupName = "BACKGROUND",
                        AttributeKey = "AGE_GROUP",
                        AttributeLabel = "Độ tuổi của người bị cắn",
                        Name = "Trẻ em (dưới 12 tuổi)",
                        DisplayOrder = 1,
                        Category = SymptomCategory.Modifier,
                        IsCritical = true,
                        AlertMessage = "⚠️ CẢNH BÁO: Trẻ em có cơ thể nhỏ, nọc độc lan nhanh và nguy hiểm gấp nhiều lần người lớn. Cần theo dõi sát!",
                        Description = "Cơ thể trẻ nhỏ, tỷ lệ độc/cân nặng cao",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 20 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 102,
                        GroupName = "BACKGROUND",
                        AttributeKey = "AGE_GROUP",
                        AttributeLabel = "Độ tuổi của người bị cắn",
                        Name = "Người cao tuổi (trên 65 tuổi)",
                        DisplayOrder = 1,
                        Category = SymptomCategory.Modifier,
                        Description = "Sức đề kháng yếu, nguy cơ biến chứng cao",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 15 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 103,
                        GroupName = "BACKGROUND",
                        AttributeKey = "AGE_GROUP",
                        AttributeLabel = "Độ tuổi của người bị cắn",
                        Name = "Người trưởng thành (12-65 tuổi)",
                        DisplayOrder = 1,
                        Category = SymptomCategory.Modifier,
                        Description = "Độ tuổi có sức đề kháng tốt nhất",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 0 }
                        })
                    },

                    // CÂU HỎI 2: Tiền sử bệnh (DisplayOrder = 2)
                    new SymptomConfig
                    {
                        Id = 104,
                        GroupName = "BACKGROUND",
                        AttributeKey = "MEDICAL_HISTORY",
                        AttributeLabel = "Bệnh nền của người bị cắn",
                        Name = "Mắc bệnh tim mạch",
                        DisplayOrder = 2,
                        Category = SymptomCategory.Modifier,
                        Description = "Tăng nguy cơ sốc tuần hoàn",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 15 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 105,
                        GroupName = "BACKGROUND",
                        AttributeKey = "MEDICAL_HISTORY",
                        AttributeLabel = "Bệnh nền của người bị cắn",
                        Name = "Mắc bệnh tiểu đường",
                        DisplayOrder = 2,
                        Category = SymptomCategory.Modifier,
                        Description = "Vết thương lành chậm, dễ hoại tử",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 15 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 106,
                        GroupName = "BACKGROUND",
                        AttributeKey = "MEDICAL_HISTORY",
                        AttributeLabel = "Bệnh nền của người bị cắn",
                        Name = "Mắc bệnh suy thận",
                        DisplayOrder = 2,
                        Category = SymptomCategory.Modifier,
                        Description = "Thận yếu, khó thải độc tố",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 15 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 107,
                        GroupName = "BACKGROUND",
                        AttributeKey = "MEDICAL_HISTORY",
                        AttributeLabel = "Bệnh nền của người bị cắn",
                        Name = "Không có bệnh nền",
                        DisplayOrder = 2,
                        Category = SymptomCategory.Modifier,
                        Description = "Sức khỏe bình thường",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 0 }
                        })
                    },

                    // CÂU HỎI 3: Vị trí vết cắn (DisplayOrder = 3)
                    new SymptomConfig
                    {
                        Id = 110,
                        GroupName = "BACKGROUND",
                        AttributeKey = "BITE_LOCATION",
                        AttributeLabel = "Rắn cắn vào vị trí nào trên cơ thể?",
                        Name = "Đầu, cổ, mặt",
                        DisplayOrder = 3,
                        Category = SymptomCategory.Modifier,
                        IsCritical = true,
                        AlertMessage = "⚠️ VỊ TRÍ CỰC KỲ NGUY HIỂM! Gần não và đường thở. Có thể gây sưng phù nghẹt thở. Cần cấp cứu GẤP!",
                        Description = "Vùng nguy hiểm nhất - gần hệ thần kinh trung ương",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 30 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 111,
                        GroupName = "BACKGROUND",
                        AttributeKey = "BITE_LOCATION",
                        AttributeLabel = "Rắn cắn vào vị trí nào trên cơ thể?",
                        Name = "Ngực, bụng, nách, bẹn",
                        DisplayOrder = 3,
                        Category = SymptomCategory.Modifier,
                        Description = "Gần cơ quan nội tạng - nguy hiểm cao",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 25 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 112,
                        GroupName = "BACKGROUND",
                        AttributeKey = "BITE_LOCATION",
                        AttributeLabel = "Rắn cắn vào vị trí nào trên cơ thể?",
                        Name = "Bàn tay, ngón tay",
                        DisplayOrder = 3,
                        Category = SymptomCategory.Modifier,
                        Description = "Nhiều mạch máu nhỏ - độc lan nhanh",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 25 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 113,
                        GroupName = "BACKGROUND",
                        AttributeKey = "BITE_LOCATION",
                        AttributeLabel = "Rắn cắn vào vị trí nào trên cơ thể?",
                        Name = "Cánh tay, cẳng tay",
                        DisplayOrder = 3,
                        Category = SymptomCategory.Modifier,
                        Description = "Vị trí thường gặp - nguy hiểm trung bình",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 20 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 114,
                        GroupName = "BACKGROUND",
                        AttributeKey = "BITE_LOCATION",
                        AttributeLabel = "Rắn cắn vào vị trí nào trên cơ thể?",
                        Name = "Bàn chân, cẳng chân",
                        DisplayOrder = 3,
                        Category = SymptomCategory.Modifier,
                        Description = "Xa tim nhất - độc lan chậm hơn",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 12 }
                        })
                    },

                    // ==================================================================================
                    // NHÓM 2: LOCAL - TRIỆU CHỨNG TẠI CHỖ (DisplayOrder: 11)
                    // ==================================================================================

                    // CÂU HỎI 4: Triệu chứng ở vết cắn (DisplayOrder = 11)
                    new SymptomConfig
                    {
                        Id = 201,
                        GroupName = "LOCAL",
                        AttributeKey = "SYMPTOM_LOCAL",
                        AttributeLabel = "Có triệu chứng gì ở vết cắn?",
                        Name = "Đau nhức dữ dội hoặc cảm giác bỏng rát",
                        DisplayOrder = 11,
                        Category = SymptomCategory.Modifier,
                        Description = "Đau là phản ứng tự nhiên khi bị rắn cắn",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 30, Score = 30 },
                            new TimeScorePoint { MinMinutes = 31, MaxMinutes = 1440, Score = 25 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 202,
                        GroupName = "LOCAL",
                        AttributeKey = "SYMPTOM_LOCAL",
                        AttributeLabel = "Có triệu chứng gì ở vết cắn?",
                        Name = "Sưng vù lan ra nhanh (> 5cm mỗi giờ)",
                        DisplayOrder = 11,
                        Category = SymptomCategory.Core,
                        VenomTypeId = 2,
                        IsCritical = true,
                        AlertMessage = "⚠️ CẢNH BÁO: Sưng lan nhanh! Có thể là Rắn Lục hoặc Hổ Mang đang phá hủy mô. KHÔNG vận động! Nằm yên chờ cứu hộ!",
                        Description = "Dấu hiệu nọc độc máu/độc tế bào nghiêm trọng",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 180, Score = 60 },
                            new TimeScorePoint { MinMinutes = 181, MaxMinutes = 1440, Score = 40 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 203,
                        GroupName = "LOCAL",
                        AttributeKey = "SYMPTOM_LOCAL",
                        AttributeLabel = "Có triệu chứng gì ở vết cắn?",
                        Name = "Bầm tím, bóng nước, phồng rộp",
                        DisplayOrder = 11,
                        Category = SymptomCategory.Modifier,
                        VenomTypeId = 2,
                        Description = "Da tổn thương do nọc độc phá hủy mô",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 30 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 204,
                        GroupName = "LOCAL",
                        AttributeKey = "SYMPTOM_LOCAL",
                        AttributeLabel = "Có triệu chứng gì ở vết cắn?",
                        Name = "Cảm giác tê, tê lan dần từ vết cắn",
                        DisplayOrder = 11,
                        Category = SymptomCategory.Modifier,
                        VenomTypeId = 1,
                        IsCritical = true,
                        AlertMessage = "⚠️ Có thể là dấu hiệu nọc độc thần kinh! Theo dõi hô hấp sát!",
                        Description = "Tê lan = độc tố thần kinh đang tấn công",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 60, Score = 30 },
                            new TimeScorePoint { MinMinutes = 61, MaxMinutes = 1440, Score = 50 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 205,
                        GroupName = "LOCAL",
                        AttributeKey = "SYMPTOM_LOCAL",
                        AttributeLabel = "Có triệu chứng gì ở vết cắn?",
                        Name = "Gần như không đau, không sưng",
                        DisplayOrder = 11,
                        Category = SymptomCategory.Modifier,
                        VenomTypeId = 1,
                        IsCritical = true,
                        AlertMessage = "⚠️ RẤT NGUY HIỂM! Đặc trưng của Rắn Cạp Nia. Vết cắn tưởng nhẹ nhưng độc thần kinh cực mạnh. Triệu chứng liệt có thể xuất hiện sau vài giờ!",
                        Description = "Không đau ≠ không nguy hiểm! Có thể là Rắn Cạp Nia",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 15 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 206,
                        GroupName = "LOCAL",
                        AttributeKey = "SYMPTOM_LOCAL",
                        AttributeLabel = "Có triệu chứng gì ở vết cắn?",
                        Name = "Da thịt chuyển đen, hoại tử, có mùi hôi",
                        DisplayOrder = 11,
                        Category = SymptomCategory.Core,
                        VenomTypeId = 3,
                        IsCritical = true,
                        AlertMessage = "🚨 NGUY CẤP! Hoại tử mô đang tiến triển nhanh. Có thể dẫn đến nhiễm trùng máu và cắt cụt chi. Cấp cứu NGAY!",
                        Description = "Mô đang chết - cần phẫu thuật khẩn cấp",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 90 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 207,
                        GroupName = "LOCAL",
                        AttributeKey = "SYMPTOM_LOCAL",
                        AttributeLabel = "Có triệu chứng gì ở vết cắn?",
                        Name = "Máu chảy liên tục, rỉ máu không ngừng",
                        DisplayOrder = 11,
                        Category = SymptomCategory.Core,
                        VenomTypeId = 2,
                        IsCritical = true,
                        AlertMessage = "🚨 Rối loạn đông máu! Cần truyền huyết thanh kháng độc khẩn cấp!",
                        Description = "Máu không đông - dấu hiệu nọc độc máu",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 85 }
                        })
                    },

                    // ==================================================================================
                    // NHÓM 3: CRITICAL - DẤU HIỆU NGUY KỊCH (DisplayOrder: 21)
                    // ==================================================================================

                    // CÂU HỎI 5: Triệu chứng nguy hiểm toàn thân (DisplayOrder = 21)
                    new SymptomConfig
                    {
                        Id = 301,
                        GroupName = "CRITICAL",
                        AttributeKey = "CORE_SIGNS",
                        AttributeLabel = "Có dấu hiệu nguy hiểm nào sau đây không?",
                        Name = "Khó thở, tức ngực, thở gấp",
                        DisplayOrder = 21,
                        Category = SymptomCategory.Core,
                        VenomTypeId = 1,
                        IsCritical = true,
                        AlertMessage = "🚨 NGUY CẤP TỐI ĐA! Suy hô hấp đang xảy ra! Gọi 115 NGAY! Chuẩn bị hỗ trợ thở nhân tạo!",
                        Description = "Dấu hiệu cơ hô hấp bị liệt",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 100 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 302,
                        GroupName = "CRITICAL",
                        AttributeKey = "CORE_SIGNS",
                        AttributeLabel = "Có dấu hiệu nguy hiểm nào sau đây không?",
                        Name = "Sụp mí mắt, mở mắt khó khăn",
                        DisplayOrder = 21,
                        Category = SymptomCategory.Core,
                        IsCritical = true,
                        AlertMessage = "🚨 Độc tố thần kinh đang tấn công! Liệt cơ mắt = sắp liệt cơ hô hấp. Giám sát hô hấp liên tục!",
                        VenomTypeId = 1,
                        Description = "Liệt cơ mắt - dấu hiệu độc thần kinh",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 60, Score = 90 },
                            new TimeScorePoint { MinMinutes = 61, MaxMinutes = 1440, Score = 80 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 303,
                        GroupName = "CRITICAL",
                        AttributeKey = "CORE_SIGNS",
                        AttributeLabel = "Có dấu hiệu nguy hiểm nào sau đây không?",
                        Name = "Chóng mặt, choáng váng, ngất xỉu",
                        DisplayOrder = 21,
                        Category = SymptomCategory.Core,
                        IsCritical = true,
                        AlertMessage = "🚨 Sốc tuần hoàn! Đặt nạn nhân nằm ngửa, nâng cao chân, giữ ấm cơ thể!",
                        Description = "Huyết áp tụt - sốc",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 30, Score = 80 },
                            new TimeScorePoint { MinMinutes = 31, MaxMinutes = 1440, Score = 70 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 304,
                        GroupName = "CRITICAL",
                        AttributeKey = "CORE_SIGNS",
                        AttributeLabel = "Có dấu hiệu nguy hiểm nào sau đây không?",
                        Name = "Chảy máu khắp cơ thể, nôn ra máu",
                        DisplayOrder = 21,
                        Category = SymptomCategory.Core,
                        VenomTypeId = 2,
                        IsCritical = true,
                        AlertMessage = "🚨 RỐI LOẠN ĐÔNG MÁU TOÀN THÂN! Nguy cơ xuất huyết nội! Cấp cứu GẤP!",
                        Description = "Máu không đông - nguy cơ chết do mất máu",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 90 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 305,
                        GroupName = "CRITICAL",
                        AttributeKey = "CORE_SIGNS",
                        AttributeLabel = "Có dấu hiệu nguy hiểm nào sau đây không?",
                        Name = "Nước tiểu màu sẫm (màu nước trà hoặc coca)",
                        DisplayOrder = 21,
                        IsCritical = true,
                        AlertMessage = "🚨 SUY THẬN CẤP! Hồng cầu đang bị phá hủy. Cần lọc máu khẩn cấp!",
                        Category = SymptomCategory.Core,
                        VenomTypeId = 4,
                        Description = "Tiểu sẫm = hồng cầu vỡ - suy thận",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 1440, Score = 80 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 306,
                        GroupName = "CRITICAL",
                        AttributeKey = "CORE_SIGNS",
                        AttributeLabel = "Có dấu hiệu nguy hiểm nào sau đây không?",
                        Name = "Buồn nôn, đau bụng dữ dội",
                        DisplayOrder = 21,
                        Category = SymptomCategory.Core,
                        Description = "Phản ứng của cơ thể với nọc độc",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 30, Score = 85 },
                            new TimeScorePoint { MinMinutes = 31, MaxMinutes = 120, Score = 70 },
                            new TimeScorePoint { MinMinutes = 121, MaxMinutes = 1440, Score = 60 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 307,
                        GroupName = "CRITICAL",
                        AttributeKey = "CORE_SIGNS",
                        AttributeLabel = "Có dấu hiệu nguy hiểm nào sau đây không?",
                        Name = "Cơ bắp yếu dần, khó cử động",
                        DisplayOrder = 21,
                        Category = SymptomCategory.Core,
                        Description = "Liệt cơ đang tiến triển",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 60, Score = 55 },
                            new TimeScorePoint { MinMinutes = 60, MaxMinutes = 1440, Score = 45 }
                        })
                    },

                    new SymptomConfig
                    {
                        Id = 308,
                        GroupName = "CRITICAL",
                        AttributeKey = "CORE_SIGNS",
                        AttributeLabel = "Có dấu hiệu nguy hiểm nào sau đây không?",
                        Name = "Khó nói, khó nuốt, khó há miệng",
                        DisplayOrder = 21,
                        Category = SymptomCategory.Core,
                        VenomTypeId = 1,
                        IsCritical = true,
                        AlertMessage = "⚠️ Liệt cơ mặt! Sắp liệt hệ hô hấp. Giám sát thở liên tục!",
                        Description = "Liệt cơ hầu họng - dấu hiệu nguy hiểm",
                        TimeScoresJson = JsonSerializer.Serialize(new List<TimeScorePoint> {
                            new TimeScorePoint { MinMinutes = 0, MaxMinutes = 60, Score = 60 },
                            new TimeScorePoint { MinMinutes = 60, MaxMinutes = 180, Score = 50 },
                            new TimeScorePoint { MinMinutes = 180, MaxMinutes = 1440, Score = 40 }
                        })
                    },
                };
                context.SymptomConfigs.AddRange(symptomConfigs);

                await context.SaveChangesAsync();
            }

            // ==================================================================================
            // SEED AI MODELS & AI SNAKE CLASS MAPPINGS
            // ==================================================================================
            // Phụ thuộc: SnakeSpecies phải được seed trước
            // Mục đích: Cho phép endpoint detect/{reportMediaId} trả về snake data đầy đủ
            // Mapping: YOLO class name -> SnakeSpecies (theo file Yolo_data.yaml)
            // ==================================================================================
            if (!context.AIModels.Any())
            {
                var aiModels = new List<AIModel>
                {
                    new AIModel
                    {
                        Id = 1,
                        Version = "7",
                        Description = "YOLO-based snake detection model trained on 22 Vietnamese snake species.",
                        IsActive = true,
                        IsDefault = true
                    }
                };
                context.AIModels.AddRange(aiModels);
                await context.SaveChangesAsync();
            }

            // ==================================================================================
            // SEED AI SNAKE CLASS MAPPINGS
            // ==================================================================================
            // Depends on: AIModels, SnakeSpecies
            // YOLO Class Name -> SnakeSpecies ID mapping (based on Yolo_data.yaml)
            // Index 1-22 corresponds with 22 classes in YOLO v7 model
            if (!context.AISnakeClassMappings.Any())
            {
                const int aiModelId = 1;
                var aiSnakeClassMappings = new List<AISnakeClassMapping>
                {
                    // YoloClassId 1: cap_nia_bac -> Rắn Cạp Nia Bắc (SnakeSpeciesId: 1)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 1, YoloClassName = "cap_nia_bac", SnakeSpeciesId = 1, IsActive = true },
                    // YoloClassId 2: cap_nia_nam -> Rắn Cạp Nia Nam (SnakeSpeciesId: 6)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 2, YoloClassName = "cap_nia_nam", SnakeSpeciesId = 6, IsActive = true },
                    // YoloClassId 3: cap_nong -> Rắn Cạp Nong (SnakeSpeciesId: 5)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 3, YoloClassName = "cap_nong", SnakeSpeciesId = 5, IsActive = true },
                    // YoloClassId 4: ho_mang_chua -> Rắn Hổ Mang Chúa (SnakeSpeciesId: 3)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 4, YoloClassName = "ho_mang_chua", SnakeSpeciesId = 3, IsActive = true },
                    // YoloClassId 5: ho_mang_xiem -> Rắn Hổ Mang Xiêm (SnakeSpeciesId: 7)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 5, YoloClassName = "ho_mang_xiem", SnakeSpeciesId = 7, IsActive = true },
                    // YoloClassId 6: khiem_vach -> Rắn Khiếm Vạch (SnakeSpeciesId: 14)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 6, YoloClassName = "khiem_vach", SnakeSpeciesId = 14, IsActive = true },
                    // YoloClassId 7: luc_cuom -> Rắn Lục Cườm (SnakeSpeciesId: 11)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 7, YoloClassName = "luc_cuom", SnakeSpeciesId = 11, IsActive = true },
                    // YoloClassId 8: luc_nua -> Rắn Lục Nưa (Chàm Quạp) (SnakeSpeciesId: 12)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 8, YoloClassName = "luc_nua", SnakeSpeciesId = 12, IsActive = true },
                    // YoloClassId 9: luc_xanh -> Rắn Lục Xanh (SnakeSpeciesId: 13)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 9, YoloClassName = "luc_xanh", SnakeSpeciesId = 13, IsActive = true },
                    // YoloClassId 10: luc_xanh_duoi_do -> Rắn Lục Đuôi Đỏ (SnakeSpeciesId: 2)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 10, YoloClassName = "luc_xanh_duoi_do", SnakeSpeciesId = 2, IsActive = true },
                    // YoloClassId 11: ran_cuom -> Rắn Cườm (Rắn Bay) (SnakeSpeciesId: 15)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 11, YoloClassName = "ran_cuom", SnakeSpeciesId = 15, IsActive = true },
                    // YoloClassId 12: ran_dai_lon -> Rắn Đai Lớn (SnakeSpeciesId: 21)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 12, YoloClassName = "ran_dai_lon", SnakeSpeciesId = 21, IsActive = true },
                    // YoloClassId 13: ran_hoa_can_van_dom -> Rắn Hoa Cân Vân Đốm (SnakeSpeciesId: 17)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 13, YoloClassName = "ran_hoa_can_van_dom", SnakeSpeciesId = 17, IsActive = true },
                    // YoloClassId 14: ran_hoa_co_do -> Rắn Hoa Cỏ Cổ Đỏ (SnakeSpeciesId: 8)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 14, YoloClassName = "ran_hoa_co_do", SnakeSpeciesId = 8, IsActive = true },
                    // YoloClassId 15: ran_rao -> Rắn Ráo (SnakeSpeciesId: 4)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 15, YoloClassName = "ran_rao", SnakeSpeciesId = 4, IsActive = true },
                    // YoloClassId 16: ran_rao_trau -> Rắn Ráo Trâu (SnakeSpeciesId: 16)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 16, YoloClassName = "ran_rao_trau", SnakeSpeciesId = 16, IsActive = true },
                    // YoloClassId 17: ran_ri_ca -> Rắn Ri Cá (SnakeSpeciesId: 18)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 17, YoloClassName = "ran_ri_ca", SnakeSpeciesId = 18, IsActive = true },
                    // YoloClassId 18: ran_roi -> Rắn Roi (SnakeSpeciesId: 19)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 18, YoloClassName = "ran_roi", SnakeSpeciesId = 19, IsActive = true },
                    // YoloClassId 19: ran_sai_co -> Rắn Sãi Cỏ (SnakeSpeciesId: 22)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 19, YoloClassName = "ran_sai_co", SnakeSpeciesId = 22, IsActive = true },
                    // YoloClassId 20: ran_soc_dua -> Rắn Hổ Ngựa (SnakeSpeciesId: 9)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 20, YoloClassName = "ran_soc_dua", SnakeSpeciesId = 9, IsActive = true },
                    // YoloClassId 21: ran_soc_go -> Rắn Chuột Vua (SnakeSpeciesId: 10)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 21, YoloClassName = "ran_soc_go", SnakeSpeciesId = 10, IsActive = true },
                    // YoloClassId 22: ran_trun -> Rắn Trun (SnakeSpeciesId: 20)
                    new AISnakeClassMapping { Id = Guid.NewGuid(), AIModelId = aiModelId, YoloClassId = 22, YoloClassName = "ran_trun", SnakeSpeciesId = 20, IsActive = true }
                };

                context.AISnakeClassMappings.AddRange(aiSnakeClassMappings);
                await context.SaveChangesAsync();
            }

            // ==================================================================================
            // SEED TREATMENT FACILITIES (HOSPITALS)
            // ==================================================================================
            // No dependencies - seed independently
            if (!context.TreatmentFacilities.Any())
            {
                var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
                var projectRoot = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "..", ".."));
                var jsonPath = Path.Combine(projectRoot, "SnakeAid.Repository", "Seeds", "hcm_hospital_data.json");
                var hospitalsJson = await File.ReadAllTextAsync(jsonPath);
                var hospitals = JsonSerializer.Deserialize<List<HospitalDto>>(hospitalsJson);

                var treatmentFacilities = hospitals.Select(h => new TreatmentFacility
                {
                    Id = h.Id,
                    Name = h.Name,
                    Address = h.Name, // Using name as address since JSON doesn't have address
                    ContactNumber = h.ContactNumber,
                    Location = _geometryFactory.CreatePoint(new Coordinate(h.Coordinates.Lng, h.Coordinates.Lat)),
                    IsActive = h.IsActive
                }).ToList();

                context.TreatmentFacilities.AddRange(treatmentFacilities);
                await context.SaveChangesAsync();
            }
        }
    }
}