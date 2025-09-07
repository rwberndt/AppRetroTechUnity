using System.Collections.Generic;

namespace RetroTech
{
    /// <summary>
    /// Static container for the sample content used throughout the RetroTech app.  In
    /// a production system these items would likely come from a database or
    /// external service, but for the purposes of this demonstration the data is
    /// hard coded to match the Flutter reference implementation.
    /// </summary>
    public static class SampleData
    {
        public static List<Category> Categories { get; private set; }
        public static List<ComputerPiece> Pieces { get; private set; }
        public static List<QuizQuestion> QuizQuestions { get; private set; }

        static SampleData()
        {
            // Initialize categories
            Categories = new List<Category>
            {
                new Category(
                    id: "calc",
                    name: "Calculadoras",
                    subcategories: new List<string> { "Calculadoras mecânicas", "Calculadoras eletrônicas" }),
                new Category(
                    id: "storage",
                    name: "Dispositivos de armazenamento",
                    subcategories: new List<string>
                    {
                        "Disquete de 5.25 polegadas",
                        "Gravador de fita cassete",
                        "Disco rígido portátil",
                        "Disquete de 8 polegadas",
                        "Bobina de fita magnética"
                    }),
                new Category(
                    id: "processors",
                    name: "Microcontroladores e Processadores",
                    subcategories: new List<string> { "Intel 8080", "Motorola 6800", "Zilog Z80" }),
                new Category(
                    id: "computers",
                    name: "Computadores pessoais e monitores",
                    subcategories: new List<string> { "Apple II", "Commodore 64", "TRS-80" }),
                new Category(
                    id: "network",
                    name: "Placas controladoras e Relês",
                    subcategories: new List<string> { "Placas de rede", "Controladores", "Relês" })
            };

            // Initialize pieces
            Pieces = new List<ComputerPiece>
            {
                new ComputerPiece(
                    id: "bobina_magnetica",
                    name: "Bobina de fita magnética",
                    category: "storage",
                    yearManufactured: 1960,
                    manufacturer: "Verbatim",
                    description: "É um meio de armazenamento usado para arquivar grandes volumes de dados em sistemas antigos e corporativos.",
                    imageUrl: "https://via.placeholder.com/300x200/9C7AB7/FFFFFF?text=Bobina+Magnetica",
                    curiosities: "A fita magnética, embora uma tecnologia antiga, ainda é usada até hoje para armazenamento de longo prazo em datacenters, especialmente para backup, isso ocorre porque as fitas magnéticas oferecem uma durabilidade superior em comparação com outros meios de armazenamento digital.",
                    specifications: new List<string> { "Capacidade: 6250 BPI", "Velocidade: 75 IPS", "Comprimento: 2400 pés" }
                ),
                new ComputerPiece(
                    id: "disquete_525",
                    name: "Disquete de 5.25 polegadas",
                    category: "storage",
                    yearManufactured: 1976,
                    manufacturer: "Shugart Associates",
                    description: "Disco flexível usado como meio de armazenamento removível em computadores pessoais dos anos 70 e 80.",
                    imageUrl: "https://via.placeholder.com/300x200/9C7AB7/FFFFFF?text=Disquete+5.25",
                    curiosities: "O disquete de 5.25\" foi revolucionário por ser menor que os de 8\", mas mantinha boa capacidade de armazenamento. Era comum ver pessoas carregando dezenas deles em estojos especiais.",
                    specifications: new List<string> { "Capacidade: 160KB-1.2MB", "Rotação: 300 RPM", "Trilhas: 40-80" }
                ),
                new ComputerPiece(
                    id: "intel_8080",
                    name: "Processador Intel 8080",
                    category: "processors",
                    yearManufactured: 1974,
                    manufacturer: "Intel",
                    description: "Microprocessador de 8 bits que foi fundamental para o desenvolvimento dos primeiros computadores pessoais.",
                    imageUrl: "https://via.placeholder.com/300x200/9C7AB7/FFFFFF?text=Intel+8080",
                    curiosities: "O Intel 8080 foi o processador usado no primeiro computador pessoal comercialmente bem-sucedido, o Altair 8800. Custava US$ 360 na época, equivalente a mais de US$ 1.500 hoje.",
                    specifications: new List<string> { "Clock: 2 MHz", "Arquitetura: 8 bits", "Transistores: 6000" }
                ),
                new ComputerPiece(
                    id: "calculadora_hp35",
                    name: "Calculadora HP-35",
                    category: "calc",
                    yearManufactured: 1972,
                    manufacturer: "Hewlett-Packard",
                    description: "A primeira calculadora científica portátil do mundo, revolucionando os cálculos de engenharia.",
                    imageUrl: "https://via.placeholder.com/300x200/9C7AB7/FFFFFF?text=HP-35",
                    curiosities: "A HP-35 foi chamada de \"slide rule killer\" porque substituiu as réguas de cálculo usadas por engenheiros. Custava US$ 395, o equivalente a um carro pequeno na época.",
                    specifications: new List<string> { "Funções: 35", "Display: LED vermelho", "Bateria: NiCad recarregável" }
                ),
                new ComputerPiece(
                    id: "apple_ii",
                    name: "Apple II",
                    category: "computers",
                    yearManufactured: 1977,
                    manufacturer: "Apple Computer",
                    description: "Um dos primeiros computadores pessoais altamente bem-sucedidos, conhecido por sua facilidade de uso.",
                    imageUrl: "https://via.placeholder.com/300x200/9C7AB7/FFFFFF?text=Apple+II",
                    curiosities: "O Apple II foi o primeiro computador pessoal a ter gráficos coloridos e som integrado. Permaneceu em produção por quase 17 anos, um recorde na indústria.",
                    specifications: new List<string> { "CPU: MOS 6502 1MHz", "RAM: 4KB-48KB", "Cores: 6 cores" }
                ),
                new ComputerPiece(
                    id: "disco_rigido_5mb",
                    name: "Disco Rígido 5MB",
                    category: "storage",
                    yearManufactured: 1980,
                    manufacturer: "Seagate",
                    description: "Um dos primeiros discos rígidos para computadores pessoais, com impressionantes 5MB de capacidade.",
                    imageUrl: "https://via.placeholder.com/300x200/9C7AB7/FFFFFF?text=HD+5MB",
                    curiosities: "Este disco custava US$ 1.500 e pesava 5kg. Para comparação, hoje você pode comprar um HD de 1TB (200.000 vezes maior) por menos de US$ 50.",
                    specifications: new List<string> { "Capacidade: 5MB", "Interface: ST-506", "Rotação: 3600 RPM" }
                ),
                new ComputerPiece(
                    id: "bobina_magnetica",
                    name: "Bobina de fita magnética",
                    category: "storage",
                    yearManufactured: 1960,
                    manufacturer: "Verbatim",
                    description: "É um meio de armazenamento usado para arquivar grandes volumes de dados em sistemas antigos e corporativos.",
                    imageUrl: "https://via.placeholder.com/300x200/9C7AB7/FFFFFF?text=Bobina+Magnetica",
                    curiosities: "A fita magnética, embora uma tecnologia antiga, ainda é usada até hoje para armazenamento de longo prazo em datacenters, especialmente para backup, isso ocorre porque as fitas magnéticas oferecem uma durabilidade superior em comparação com outros meios de armazenamento digital.",
                    specifications: new List<string> { "Capacidade: 6250 BPI", "Velocidade: 75 IPS", "Comprimento: 2400 pés" }
                ),
                new ComputerPiece(
                    id: "bobina_magnetica",
                    name: "Bobina de fita magnética",
                    category: "storage",
                    yearManufactured: 1960,
                    manufacturer: "Verbatim",
                    description: "É um meio de armazenamento usado para arquivar grandes volumes de dados em sistemas antigos e corporativos.",
                    imageUrl: "https://via.placeholder.com/300x200/9C7AB7/FFFFFF?text=Bobina+Magnetica",
                    curiosities: "A fita magnética, embora uma tecnologia antiga, ainda é usada até hoje para armazenamento de longo prazo em datacenters, especialmente para backup, isso ocorre porque as fitas magnéticas oferecem uma durabilidade superior em comparação com outros meios de armazenamento digital.",
                    specifications: new List<string> { "Capacidade: 6250 BPI", "Velocidade: 75 IPS", "Comprimento: 2400 pés" }
                ),
            };

            // Initialize quiz questions
            QuizQuestions = new List<QuizQuestion>
            {
                new QuizQuestion(
                    id: "q1",
                    question: "Qual foi o primeiro processador de 8 bits da Intel?",
                    options: new List<string> { "Intel 8008", "Intel 8080", "Intel 8086", "Intel 4004" },
                    correctAnswerIndex: 1,
                    explanation: "O Intel 8080, lançado em 1974, foi o sucessor do 8008 e se tornou extremamente popular nos primeiros computadores pessoais.",
                    relatedPieceId: "intel_8080"
                ),
                new QuizQuestion(
                    id: "q2",
                    question: "Qual a capacidade de um disquete de 5.25 polegadas de alta densidade?",
                    options: new List<string> { "360KB", "720KB", "1.2MB", "1.44MB" },
                    correctAnswerIndex: 2,
                    explanation: "Os disquetes de 5.25\" de alta densidade podiam armazenar 1.2MB, enquanto os de baixa densidade armazenavam 360KB.",
                    relatedPieceId: "disquete_525"
                ),
                new QuizQuestion(
                    id: "q3",
                    question: "Em que ano foi lançado o Apple II?",
                    options: new List<string> { "1975", "1976", "1977", "1978" },
                    correctAnswerIndex: 2,
                    explanation: "O Apple II foi lançado em 1977 e se tornou um dos computadores pessoais mais bem-sucedidos da história.",
                    relatedPieceId: "apple_ii"
                ),
                new QuizQuestion(
                    id: "q4",
                    question: "Quanto custava a calculadora HP-35 quando foi lançada em 1972?",
                    options: new List<string> { "US$ 195", "US$ 295", "US$ 395", "US$ 495" },
                    correctAnswerIndex: 2,
                    explanation: "A HP-35 custava US$ 395 em 1972, o que equivale a mais de US$ 2.000 em valores atuais.",
                    relatedPieceId: "calculadora_hp35"
                ),
                new QuizQuestion(
                    id: "q5",
                    question: "Qual era a capacidade dos primeiros discos rígidos para PCs?",
                    options: new List<string> { "1MB", "5MB", "10MB", "20MB" },
                    correctAnswerIndex: 1,
                    explanation: "O Seagate ST-506, um dos primeiros HDs para PCs, tinha 5MB de capacidade e custava cerca de US$ 1.500.",
                    relatedPieceId: "disco_rigido_5mb"
                )
            };
        }
    }
}