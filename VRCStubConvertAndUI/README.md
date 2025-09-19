# VRChat SDK3 Stub Converters and UI System

A comprehensive UI and conversion system for CVRFury that provides Unity Editor interfaces and automated conversion functionality for VRC SDK components to CVR equivalents. This system works in conjunction with the VRCSDK3Stub DLLs to provide a complete conversion pipeline.

## Overview

This system provides:
- **Custom Unity Editor UI** for VRC stub components
- **Automated conversion tools** to transform VRC components to CVR equivalents
- **Shared UI styling** system for consistent CVRFury branding
- **Modular architecture** allowing independent updates without recompiling core stubs

## Architecture

The system is built with a modular architecture consisting of:

### Core Components

#### VRCStubUICore
The foundation library providing:
- **Shared UI styles** and CSS classes for consistent CVRFury theming
- **Common utilities** used across all converter modules
- **Base functionality** that other converters depend on

### Converter Modules

Each converter module provides both UI and conversion functionality for specific VRC component categories:

#### VRCAVUIAndConverter (Avatar Components)
Handles conversion and UI for:
- **VRCAvatarDescriptor** → CVRAvatarDescriptor conversion with collider info mapping
- **VRCSpatialAudioSource** → CVR audio source equivalent
- **VRCHeadChop** → CVR head chop functionality
- **VRCAvatarParameterDriver** → CVR parameter driver conversion
- **VRCExpressionParameters** → CVR parameter system mapping
- **VRCExpressionsMenu** → CVR menu system conversion

#### VRCPBUIAndConverter (PhysBones)
Provides conversion for:
- **VRCPhysBone** → CVR PhysBone with parameter mapping and collision detection
- **VRCPhysBoneCollider** → CVR collider system with proper transform handling

#### VRCPCUIAndConverter (Contact System)
Handles contact component conversion:
- **VRCContactReceiver** → CVR touch receiver with parameter bindings
- **VRCContactSender** → CVR touch sender with collision layer mapping

#### VRCPConUIAndConverter (Constraints)
Converts VRC constraint system:
- **VRCAimConstraint** → CVR aim constraint with target mapping
- **VRCLookAtConstraint** → CVR look-at constraint conversion
- **VRCParentConstraint** → CVR parent constraint with weight handling
- **VRCPositionConstraint** → CVR position constraint mapping
- **VRCRotationConstraint** → CVR rotation constraint conversion
- **VRCScaleConstraint** → CVR scale constraint transformation

#### VRCPMUIAndConverter (Pipeline Management)
Manages:
- **PipelineManager** → CVR pipeline equivalent for avatar upload management

## Key Features

### Intelligent Component Conversion
- **Automatic parameter mapping** between VRC and CVR systems
- **Collision layer translation** for PhysBones and Contact components
- **Transform hierarchy preservation** during conversion
- **Asset reference updating** to maintain functionality

### Enhanced Editor UI
- **Custom property drawers** for all VRC stub components
- **Convert buttons** integrated directly into component inspectors
- **Visual feedback** during conversion process
- **Consistent CVRFury branding** across all editors

### Safety Features
- **Non-destructive conversion** - original VRC components are preserved
- **Dependency validation** to ensure CVR SDK is available
- **Error handling** with user-friendly messages
- **Undo support** for all conversion operations

## Build Output

The system generates the following DLLs in the `Build/` directory:

- `VRCStubUICore.dll` - Core UI system and shared utilities
- `VRCAVUIAndConverter.dll` - Avatar component converters and UI
- `VRCPBConverter.dll` - PhysBone conversion functionality  
- `VRCPCConverter.dll` - Contact system converters
- `VRCPConConverter.dll` - Constraint system converters
- `VRCPMConverter.dll` - Pipeline management tools

## Dependencies

### Required for Operation
- **CVR SDK** must be installed in the project
- **VRCSDK3Stub DLLs** for component definitions

### Build Dependencies
- **.NET Framework 4.7.1** or later
- **Unity Editor references** for custom editor functionality
- **CVR.CCK** assembly references for target component types

## Usage Workflow

1. **Import VRC Avatar** with VRC SDK components
2. **Add VRCStub DLLs** to project (from VRCSDK3Stub build)
3. **Add Converter DLLs** to project (from this build)
4. **Select VRC components** in inspector
5. **Click "Convert to CVR"** buttons in enhanced UI
6. **Verify conversion** results and test functionality

## Development Notes

- Each converter module is **independently buildable** allowing targeted updates
- **UI Toolkit** is used for modern, responsive editor interfaces  
- **Reflection-based** component discovery ensures compatibility across CVR SDK versions
- **Extensive error handling** provides clear feedback during conversion failures

## Warnings

- **CVR SDK must be installed** before using conversion functionality
- **VRC SDK should NOT be installed** alongside the stub system
- **Always backup projects** before running mass conversions
- **Test converted components** thoroughly before finalizing avatar builds