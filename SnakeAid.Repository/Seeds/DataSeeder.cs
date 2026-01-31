using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;

namespace SnakeAid.Repository.Seeds
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(SnakeAidDbContext context)
        {
            // Seed VenomTypes based on PrimaryVenomType enum
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

                var guidelines = new List<FirstAidGuideline>
                {
                    // 1. SƠ CỨU CHUNG
                    new FirstAidGuideline {
                        Id = 1,
                        Name = "Sơ cứu cơ bản",
                        Summary = "Các bước an toàn cho mọi trường hợp bị rắn cắn.",
                        Content = JsonSerializer.Serialize(new {
                            steps = new[] {
                                new { text = "Di chuyển nhẹ nhàng rời xa khu vực có rắn.", mediaUrl = "https://www.wikihow.com/images/thumb/3/39/Treat-a-Rattlesnake-Bite-Step-1-Version-4.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-1-Version-4.jpg" },
                                new { text = "Nằm yên, giữ vết cắn thấp hơn tim, hít thở đều để giữ bình tĩnh.", mediaUrl = "https://www.wikihow.com/images/thumb/d/da/Treat-a-Rattlesnake-Bite-Step-3-Version-3.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-3-Version-3.jpg" },
                                new { text = "Rửa vết cắn nhẹ nhàng bằng nước sạch.", mediaUrl = "" },
                                new { text = "Dùng nẹp cố định lỏng tay/chân bị cắn.", mediaUrl = "https://benhvienhuulung.vn/images/upload/v4-460px-Treat-a-Snake-Bite-Step-10-Version-5.jpg" },
                                new { text = "Gọi hỗ trợ y tế sớm nhất có thể.", mediaUrl = "https://dichvuxecuuthuong115.com/upload/images/goi-cap-cuu-115.jpg" },
                                new { text = "Đưa đến bệnh viện ngay lập tức.", mediaUrl = "https://png.pngtree.com/png-vector/20240216/ourmid/pngtree-flat-hospital-icon-building-vector-png-image_11740947.png" }
                            },
                            dos = new[] {
                                new { text = "Tháo nhẫn, đồng hồ, đồ chật gần vết cắn.", mediaUrl = "https://www.wikihow.com/images/thumb/2/27/Treat-a-Rattlesnake-Bite-Step-5-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-5-Version-2.jpg.webp" },
                                new { text = "Chụp ảnh rắn nếu an toàn", mediaUrl = "" }
                            },
                            donts = new[] {
                                new { text = "Không rạch, hút nọc", mediaUrl = "https://www.wikihow.com/images/thumb/7/75/Treat-a-Rattlesnake-Bite-Step-15-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-15-Version-2.jpg.webp" },
                                new { text = "Không chườm đá, đắp lá", mediaUrl = "https://cdn2.tuoitre.vn/zoom/480_300/1200/900/ttc/r/2021/07/15/image001-1626321686.jpg" },
                                new { text = "Không uống rượu/bia", mediaUrl = "https://www.mediplus.vn/wp-content/uploads/2021/10/truoc-khi-xet-nghiem-covid-19-duoc-uong-ruou-khong.jpg" }
                            }
                        })
                    },

                    // 2. ĐỘC THẦN KINH
                    new FirstAidGuideline {
                        Id = 2,
                        Name = "Sơ cứu Độc thần kinh",
                        Summary = "Ngăn chặn liệt hô hấp bằng cách băng cố định đúng cách.",
                        Content = JsonSerializer.Serialize(new {
                            steps = new[] {
                                new { text = "Di chuyển nhẹ nhàng rời khỏi khu vực có rắn.", mediaUrl = "https://www.wikihow.com/images/thumb/3/39/Treat-a-Rattlesnake-Bite-Step-1-Version-4.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-1-Version-4.jpg" },
                                new { text = "Gọi hỗ trợ y tế sớm nhất có thể.", mediaUrl = "https://dichvuxecuuthuong115.com/upload/images/goi-cap-cuu-115.jpg" },
                                new { text = "Quấn băng thun quanh vết cắn và toàn bộ tay/chân (chặt như băng bong gân).", mediaUrl = "" },
                                new { text = "Quấn từ ngón tay/chân đi ngược dần lên phía nách hoặc háng.", mediaUrl = "" },
                                new { text = "Dùng nẹp cố định để tay/chân không thể cử động.",
                                mediaUrl = "https://hscc.vn/hinhanh/randoccan_socuu.png" },
                                new { text = "Nằm yên và di chuyển bằng cáng. Tuyệt đối không tự đi bộ.", mediaUrl = "https://lh5.googleusercontent.com/i8pGvoLht7TcUieukFfbJgxyhfSjBKKh6HgjaBdOG949U2qn7JdQ4HApvHFebdFG5zpP2nrNwCfESg2yzAqZXSXW_aOXRe_lnsSeBgfyTtIXbiIBOiTUj4kvlVcPiqFDY6xOz0w"}
                            },
                            dos = new[] {
                                new { text = "Kiểm tra mạch ngọn chi (đảm bảo máu vẫn lưu thông)", mediaUrl = "" },
                                new { text = "Giữ nguyên băng quấn cho tới khi gặp bác sĩ", mediaUrl = "https://www.wikihow.com/images/thumb/f/fa/Treat-a-Rattlesnake-Bite-Step-9-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-9-Version-2.jpg" }
                            },
                            donts = new[] {
                                new { text = "Tuyệt đối không tự ý tháo băng quấn", mediaUrl = "https://www.wikihow.com/images/thumb/9/90/Treat-a-Rattlesnake-Bite-Step-8-Version-3.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-8-Version-3.jpg.webp" },
                                new { text = "Không để nạn nhân cử động tay chân", mediaUrl = "https://www.wikihow.com/images/thumb/6/68/Treat-a-Rattlesnake-Bite-Step-4-Version-4.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-4-Version-4.jpg" }
                            },
                            notes = new[] { "Cảnh báo: Độc này có thể gây liệt cơ thở rất nhanh. Chú ý hỗ trợ hô hấp kịp thời." }
                        })
                    },

                    // 3. ĐỘC MÁU
                    new FirstAidGuideline {
                        Id = 3,
                        Name = "Sơ cứu Độc máu",
                        Summary = "Ngăn chảy máu và bảo vệ hệ tuần hoàn. (Không được quấn chặt)",
                        Content = JsonSerializer.Serialize(new {
                            steps = new[] {
                                new { text = "Rửa vết cắn nhẹ nhàng bằng nước sạch.", mediaUrl = "" },
                                new { text = "Dùng nẹp cố định tay/chân nhưng quấn lỏng tay (tuyệt đối không siết chặt).", mediaUrl = "" },
                                new { text = "Giữ vùng bị cắn nằm ngang mức với tim.", mediaUrl = "https://www.wikihow.com/images/thumb/d/da/Treat-a-Rattlesnake-Bite-Step-3-Version-3.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-3-Version-3.jpg" },
                                new { text = "Đưa nạn nhân đến bệnh viện khẩn cấp.", mediaUrl = "https://png.pngtree.com/png-vector/20240216/ourmid/pngtree-flat-hospital-icon-building-vector-png-image_11740947.png" }
                            },
                            dos = new[] {
                                new { text = "Tháo trang sức ngay (tránh sưng nề gây thắt mạch máu)", mediaUrl ="https://www.wikihow.com/images/thumb/2/27/Treat-a-Rattlesnake-Bite-Step-5-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-5-Version-2.jpg.webp" },
                                new { text = "Theo dõi các vết bầm tím", mediaUrl = "https://www.wikihow.com/images/thumb/c/cd/Treat-a-Rattlesnake-Bite-Step-11-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-11-Version-2.jpg" }
                            },
                            donts = new[] {
                                new { text = "Không băng ép chặt (Garrot)", mediaUrl = "https://www.wikihow.com/images/thumb/4/47/Treat-a-Rattlesnake-Bite-Step-16-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-16-Version-2.jpg" },
                                new { text = "Không dùng thuốc giảm đau như Aspirin", mediaUrl = "https://trungtamthuoc.com/images/products/aspirin-100-traphaco-l4575.jpg" }
                            },
                            notes = new[] { "Dấu hiệu: Chảy máu chân răng, tiểu đỏ, nôn ra máu." }
                        })
                    },

                    // 4. ĐỘC TẾ BÀO
                    new FirstAidGuideline {
                        Id = 4,
                        Name = "Sơ cứu Độc tế bào",
                        Summary = "Ngăn ngừa thối rữa mô và hoại tử. (Không được quấn chặt)",
                        Content = JsonSerializer.Serialize(new {
                            steps = new[] {
                                new { text = "Rửa sạch vết thương và để thoáng mát.", mediaUrl = "https://png.pngtree.com/png-vector/20200325/ourlarge/pngtree-hand-wash-vector-icons-illustration-png-image_2164976.jpg" },
                                new { text = "Dùng nẹp cố định tay/chân nhưng quấn lỏng tay.", mediaUrl = "" },
                                new { text = "Giữ vùng bị cắn nằm ngang mức với tim.", mediaUrl = "" },
                                new { text = "Đưa đi cấp cứu sớm nhất có thể.", mediaUrl = "https://png.pngtree.com/png-vector/20240216/ourmid/pngtree-flat-hospital-icon-building-vector-png-image_11740947.png" }
                            },
                            dos = new[] {
                                new { text = "Tháo mọi vật gây thắt chi (nhẫn, vòng)", mediaUrl = "https://www.wikihow.com/images/thumb/2/27/Treat-a-Rattlesnake-Bite-Step-5-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-5-Version-2.jpg.webp" },
                                new { text = "Theo dõi vùng da bị đổi màu hoặc phồng rộp", mediaUrl = "https://www.wikihow.com/images/thumb/c/cd/Treat-a-Rattlesnake-Bite-Step-11-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-11-Version-2.jpg" }
                            },
                            donts = new[] {
                                new { text = "Không chườm đá lạnh trực tiếp", mediaUrl = "" },
                                new { text = "Không quấn băng chặt quanh vết cắn (nhanh hoại tử)", mediaUrl = "https://www.wikihow.com/images/thumb/4/47/Treat-a-Rattlesnake-Bite-Step-16-Version-2.jpg/v4-728px-Treat-a-Rattlesnake-Bite-Step-16-Version-2.jpg" }
                            },
                            notes = new[] { "Dấu hiệu: Vết cắn sưng vù rất nhanh, da thâm đen." }
                        })
                    },

                    // 5. ĐỘC CƠ
                    new FirstAidGuideline {
                        Id = 5,
                        Name = "Sơ cứu Độc cơ",
                        Summary = "Bảo vệ cơ bắp và ngăn suy thận cấp. (Cần quấn băng + uống nước)",
                        Content = JsonSerializer.Serialize(new {
                            steps = new[] {
                                new { text = "Quấn băng thun quanh vết cắn và toàn bộ tay/chân (chặt như băng bong gân).", mediaUrl = "" },
                                new { text = "Quấn từ ngón tay/chân đi ngược dần lên phía nách hoặc háng.", mediaUrl = "https://hscc.vn/hinhanh/randoccan_socuu.png" },
                                new { text = "Uống thật nhiều nước (nếu còn tỉnh táo) để giúp thận thải độc.", mediaUrl = "https://karofi.karofi.com/karofi-com/2019/12/uong-nuoc-karofi-2.jpg.webp" },
                                new { text = "Vận chuyển bằng cáng đến bệnh viện có máy lọc thận gấp.", mediaUrl = "https://png.pngtree.com/png-vector/20240216/ourmid/pngtree-flat-hospital-icon-building-vector-png-image_11740947.png" }
                            },
                            dos = new[] {
                                new { text = "Giữ ấm cơ thể nạn nhân", mediaUrl = "" },
                                new { text = "Theo dõi màu nước tiểu", mediaUrl = "" }
                            },
                            donts = new[] {
                                new { text = "Không vận động cơ bắp", mediaUrl = "https://lh5.googleusercontent.com/i8pGvoLht7TcUieukFfbJgxyhfSjBKKh6HgjaBdOG949U2qn7JdQ4HApvHFebdFG5zpP2nrNwCfESg2yzAqZXSXW_aOXRe_lnsSeBgfyTtIXbiIBOiTUj4kvlVcPiqFDY6xOz0w" },
                                new { text = "Không dùng thuốc giảm đau bừa bãi", mediaUrl = "https://trungtamthuoc.com/images/products/aspirin-100-traphaco-l4575.jpg" }
                            },
                            notes = new[] { "Dấu hiệu: Đau nhức cơ toàn thân, nước tiểu màu nâu/đen như xá xị." }
                        })
                    }
                };
                context.FirstAidGuidelines.AddRange(guidelines);
                await context.SaveChangesAsync();
            }
        }
    }
}