Tabla CC:

[CC](
	[NC] [varchar](100) NOT NULL,
	[NN] [varchar](100) NULL,
	[BANCO] [varchar](50) NULL,
	[CIERRE] [datetime] NULL,
	[TIPO] [int] NULL,
	[CLIENTE] [varchar](100) NULL,
	[ID] [int] NOT NULL,
	[ESTADO] [varchar](50) NULL,
	[IDBUZON] [int] NULL,
	[DIRECCION] [varchar](1000) NULL,
	[IDCLIENTE] [int] NULL,
	[VENCECONTRATO] [datetime] NULL,
	[EMAIL] [varchar](100) NULL,
	[MONTOMAXIMO] [float] NULL,
	[SUCURSAL] [varchar](150) NULL,
	[TANDA] [int] NULL,
	[TELEFONO] [varchar](150) NULL,
	[SUCURSALRECUENTO] [varchar](150) NULL,
	[INSTALACION] [varchar](50) NULL,
	[ARCHIVO] [varchar](200) NULL,
	[ARCHIVOSEGURO] [varchar](200) NULL,
	[FECHAINST] [datetime] NULL,
	[FECHABAJA] [datetime] NULL,
	[TIPOEXPORTACION] [int] NULL,
	[MONTOMINIMO] [float] NULL,
	[PUNTOAPUNTO] [int] NULL,
	[IDCC] [varchar](5) NULL,
	[IDTIPOBUZON] [int] NULL,
	[SEGUROACTIVO] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NC] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


Tabla  CuentasBuzones:

[dbo].[CUENTASBUZONES](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[SUCURSAL] [varchar](20) NULL,
	[CUENTA] [varchar](100) NULL,
	[MONEDA] [varchar](100) NULL,
	[BANCO] [varchar](100) NULL,
	[EMPRESA] [varchar](100) NULL,
	[IDCLIENTE] [int] NULL,
	[TANDA] [int] NULL,
	[TIPO] [varchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

Tabla configuracionacreditacion:

[dbo].[ConfiguracionAcreditacion](
	[ConfigId] [int] IDENTITY(1,1) NOT NULL,
	[CuentasBuzonesId] [int] NOT NULL,
	[TipoAcreditacion] [varchar](20) NULL,
	[NC] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[ConfigId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ConfiguracionAcreditacion]  WITH CHECK ADD FOREIGN KEY([CuentasBuzonesId])
REFERENCES [dbo].[CUENTASBUZONES] ([ID])
GO


Tabla de acreditaciones en produccion:

[AcreditacionDepositoDiegoTest](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IDBUZON] [varchar](50) NOT NULL,
	[IDOPERACION] [bigint] NOT NULL,
	[FECHA] [datetime] NULL,
	[IDBANCO] [int] NULL,
	[IDCUENTA] [int] NOT NULL,
	[MONEDA] [int] NOT NULL,
	[NO_ENVIADO] [bit] NOT NULL,
	[MONTO] [float] NOT NULL,
	[FECHADEP] [datetime] NULL,
	[NSU] [int] NULL,
	[NOMBRE_ARCHIVO] [nvarchar](255) NULL,
 CONSTRAINT [PK_AcreditacionDepositoDiegoTest] PRIMARY KEY CLUSTERED 
(
	[IDBUZON] ASC,
	[IDOPERACION] ASC,
	[MONEDA] ASC,
	[IDCUENTA] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[AcreditacionDepositoDiegoTest] ADD  DEFAULT ((0)) FOR [NO_ENVIADO]
GO

ALTER TABLE [dbo].[AcreditacionDepositoDiegoTest] ADD  DEFAULT ((0)) FOR [MONTO]
GO

ALTER TABLE [dbo].[AcreditacionDepositoDiegoTest]  WITH CHECK ADD FOREIGN KEY([IDBANCO])
REFERENCES [dbo].[BANCOS] ([ID])
GO


Tabla DEpositos( Base de datos WebBuzones 10.0.0.4)

Depositos](
	[IdDeposito] [int] IDENTITY(1,2) NOT NULL,
	[IdOperacion] [int] NOT NULL,
	[Codigo] [varchar](80) NOT NULL,
	[Empresa] [varchar](80) NOT NULL,
	[Folio] [int] NULL,
	[Tipo] [varchar](50) NOT NULL,
	[Usuario] [varchar](80) NOT NULL,
	[Zarea] [varchar](80) NOT NULL,
	[FechaDep] [datetime] NOT NULL,
	[RV] [bit] NOT NULL,
	[FechaActualizacion] [datetime] NOT NULL,
	[NSU] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdDeposito] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Depositos] ADD  DEFAULT ((0)) FOR [RV]
GO

ALTER TABLE [dbo].[Depositos] ADD  DEFAULT (getdate()) FOR [FechaActualizacion]
GO

ALTER TABLE [dbo].[Depositos] ADD  CONSTRAINT [DF_Depositos_NSU]  DEFAULT ((0)) FOR [NSU]
GO


