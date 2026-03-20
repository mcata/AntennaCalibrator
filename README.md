# AntennaCalibrator

> Low-cost GNSS antenna calibration for differential positioning: an innovative approach based on genetic algorithms

## Description

**AntennaCalibrator** is a software application designed to address the calibration problem of low-cost GNSS (Global Navigation Satellite System) antennas. It introduces an innovative approach based on genetic algorithms to improve differential positioning performance in applications requiring centimeter-level accuracy.

### The Problem

Low-cost GNSS antennas exhibit significant Phase Center Variation (PCV) differences between individual units, making the use of standard average calibrations (such as those provided in the IGS ANTEX file) impractical. Traditional absolute calibration requires expensive robotic arms and is economically viable only for high-precision geodetic antennas.

### The Solution

This software implements a genetic algorithm aimed at automatically extracting Phase Center Variation (PCV) parameters from real GNSS observations.
It uses RTKLIB as the processing engine for differential computation and optimizes calibration parameters by minimizing positioning errors.

## Architecture

```
AntennaCalibrator/
├── AntennaCalibrator.Host/           # Main console application (genetic algorithm)
│   ├── GA/                           # Genetic algorithm implementation
│   │   ├── GeneticAlgorithm.cs       # GA main loop
│   │   ├── Chromosome.cs             # Chromosome representation (22 genes)
│   │   ├── Fitness.cs                # Fitness evaluation using RTKLIB
│   │   ├── SteadyStatePopulation.cs  # Steady-state population management
│   │   ├── SbxCrossover.cs           # SBX (Simulated Binary Crossover)
│   │   ├── GaussianMutation.cs       # Gaussian mutation
│   │   └── RouletteWheelSelection.cs # Roulette wheel selection
│   ├── Clustering/                   # Clustering algorithms for diversity
│   │   └── KMeansClustering.cs       # K-means for duplicate removal
│   ├── Utilis/                       # Utilities
│   │   ├── Configuration.cs          # XML config deserialization
│   │   ├── FileManager.cs            # RINEX/ANTEX file management
│   │   ├── ExternalTools.cs          # RTKLIB integration
│   │   └── PipeSenderService.cs      # UI communication
│   └── Ancillary/                    # Configuration files
│       ├── config/config.xml         # Main configuration
│       └── sw/                       # RTKLIB executables
├── AntennaCalibrator.View/           # .NET MAUI user interface
```

## Genetic Algorithm

### Chromosome Structure

Each chromosome represents an antenna calibration configuration with **22 genes**:

| Index | Description                                                  |
| ----- | ------------------------------------------------------------ |
| 0-2   | PCO (Phase Center Offset) – N, E, U components in mm         |
| 3     | Fixed to 0.0 (calibration constraint)                        |
| 4-21  | PCV (Phase Center Variation) for 18 elevation bands (0°–90°) |

### Genetic Operators

* **Selection**: Roulette Wheel Selection
* **Crossover**: SBX (Simulated Binary Crossover) with adaptive probability [0.6, 0.9]
* **Mutation**: Gaussian mutation with adaptive probability [0.01, 0.1]
* **Reinsertion**: Steady-state with elitism
* **Diversification**: K-means clustering for removal of similar individuals
* **Local Search**: Coordinate descent for solution refinement

### Fitness Function

The fitness function combines two normalized components:

1. **Residual RMSE** across elevation bands (19 bands from 0° to 90°)
2. **Combined standard deviation** of XYZ coordinates

Formula:

```
fitness = 100 × (0.5 × normalizedRMSE + 0.5 × normalizedStdDev)
```

## Requirements

### Dependencies

* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* [RTKLIB](https://rtklib.com/) (included in `Ancillary/sw/`)
* IGS calibration file (igs14.atx)
* RTKLIB configuration file (rnx2rtkp.conf)

### Input Data

The software requires:

* **RINEX observation file** for the rover (.rnx)
* **RINEX observation file** for the reference station (.rnx)
* **RINEX navigation file** (.rnx)
* **SP3 file** with precise ephemerides (.SP3)
* **ERP file** with Earth rotation parameters (.ERP)
* **Known position** of the reference station (ECEF coordinates)

## Usage

### Configuration

Edit the file `Ancillary/config/config.xml`:

```xml
<Configuration>
  <Generation>
    <Number>50</Number>                  <!-- Number of generations -->
    <StagnantNumber>10</StagnantNumber>  <!-- Generations for stagnation detection -->
  </Generation>
  <PopulationSize>100</PopulationSize>
  <Crossover>
    <Probability>0.8</Probability>
    <DistributionIndex>10</DistributionIndex>
  </Crossover>
  <Mutation>
    <Probability>0.05</Probability>
    <Noise>0.5</Noise>
  </Mutation>
  <!-- RINEX file paths -->
  <RoverRinex><File>path/to/rover.rnx</File></RoverRinex>
  <ReferenceRinex><File>path/to/ref.rnx</File></ReferenceRinex>
  <NavigationRinex><File>path/to/nav.rnx</File></NavigationRinex>
  <Sp3File><File>path/to/eph.sp3</File></Sp3File>
  <ErpFile><File>path/to/erp.erp</File></ErpFile>
  <!-- Known reference station position -->
  <ReferencePosition>
    <X>4641951.7049</X>
    <Y>1393053.3980</Y>
    <Z>4133280.6780</Z>
  </ReferencePosition>
  <!-- Antenna models -->
  <RoverAntenna>LEIAR20_C</RoverAntenna>
  <ReferenceAntenna>LEIAR20</ReferenceAntenna>
  <!-- Initial PCV values (optional) -->
  <StartValues>
    <Value>0.49</Value>   <!-- PCO North -->
    <Value>0.12</Value>   <!-- PCO East -->
    <Value>122.25</Value> <!-- PCO Up -->
    <Value>0.00</Value>   <!-- Fixed constraint -->
    <!-- ... 18 PCV values for elevation bands ... -->
  </StartValues>
</Configuration>
```

### Execution

```bash
# Build
dotnet build AntennaCalibrator.sln

# Run host (console)
dotnet run --project AntennaCalibrator.Host/AntennaCalibrator.csproj

# Run with UI
dotnet run --project AntennaCalibrator.View/AntennaCalibrator.View/AntennaCalibrator.View.csproj
```

### Runtime Output

During execution, the following files are generated:

* `temp/best_chromosomes.csv` – Best chromosomes per generation
* `temp/population/*.csv` – Full populations per generation
* `temp/fitness.txt` – Fitness trend
* Timestamped log files

## User Interface

The application includes a graphical interface developed with .NET MAUI that allows you to:

* Visualize generation progress in real time
* Monitor the best chromosomes
* Observe fitness statistics

## Final Output

The algorithm produces:

1. **PCV model** – Phase center variations for elevation angles
2. **Custom ANTEX file** – RTKLIB-compatible format