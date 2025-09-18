using gex.Models;
using gex.Models.Api;
using gex.Models.Bar;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/unit")]
    public class BarUnitApiController : ApiControllerBase {

        private readonly ILogger<BarUnitApiController> _Logger;
        private readonly BarUnitRepository _BarUnitRepository;
        private readonly BarWeaponDefinitionRepository _WeaponDefinitionRepository;
        private readonly BarI18nRepository _I18n;

        public BarUnitApiController(ILogger<BarUnitApiController> logger,
            BarUnitRepository barUnitRepository, BarI18nRepository i18n,
            BarWeaponDefinitionRepository weaponDefinitionRepository) {

            _Logger = logger;
            _BarUnitRepository = barUnitRepository;
            _I18n = i18n;
            _WeaponDefinitionRepository = weaponDefinitionRepository;
        }

        /// <summary>
        ///     get a list of all the names of bar units as the definition name and english display name
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="BarUnitName"/>s which represent
        ///     the names of all units in the base game of BAR
        /// </response>
        [HttpGet("all")]
        public async Task<ApiResponse<List<BarUnitName>>> GetAll(CancellationToken cancel) {
            List<BarUnitName> names = await _BarUnitRepository.GetAllNames(cancel);
            return ApiOk(names);
        }

        /// <summary>
        ///     get a list of all <see cref="ApiBarUnit"/>s
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [HttpGet("all-defs")]
        [ResponseCache(Duration = 60 * 5)]
        public async Task<ApiResponse<List<ApiBarUnit>>> GetAllDefinitions(CancellationToken cancel) {
            List<BarUnitName> names = await _BarUnitRepository.GetAllNames(cancel);

            List<ApiBarUnit> units = [];

            foreach (BarUnitName name in names) {
                if (_BarUnitRepository.HasUnit(name.DefinitionName) == false) {
                    continue;
                }

                Result<BarUnit, string> res = await _BarUnitRepository.GetByDefinitionName(name.DefinitionName, cancel);
                if (res.IsOk == false) {
                    _Logger.LogWarning($"error getting unit from repository [defName={name.DefinitionName}] [error={res.Error}]");
                } else {
                    units.Add(await _Convert(res.Value, cancel));
                }
            }

            return ApiOk(units);
        }

        /// <summary>
        ///     get a <see cref="BarUnit"/> by its <see cref="BarUnit.DefinitionName"/>
        /// </summary>
        /// <param name="defName">definition name of the unit. case-sensitive</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain the <see cref="BarUnit"/> with the <see cref="BarUnit.DefinitionName"/>
        ///     of <paramref name="defName"/>
        /// </response>
        /// <response code="204">
        ///     no unit with <see cref="BarUnit.DefinitionName"/> of <paramref name="defName"/> exists
        /// </response>
        [HttpGet("def-name/{defName}")]
        public async Task<ApiResponse<ApiBarUnit>> GetByDefinitionName(string defName,
            CancellationToken cancel = default) {

            if (_BarUnitRepository.HasUnit(defName) == false) {
                return ApiNoContent<ApiBarUnit>();
            }

            Result<BarUnit, string> res = await _BarUnitRepository.GetByDefinitionName(defName, cancel);
            if (res.IsOk == false) {
                return ApiInternalError<ApiBarUnit>($"error getting unit from repository [error={res.Error}]");
            }

            return ApiOk(await _Convert(res.Value, cancel));
        }

        private async Task<ApiBarUnit> _Convert(BarUnit def, CancellationToken cancel) {
            ApiBarUnit unit = new();
            unit.DefinitionName = def.DefinitionName;
            unit.Unit = def;
            unit.DisplayName = await _I18n.GetString("units", $"units.names.{unit.DefinitionName}", cancel) ?? $"<missing name>";
            unit.Description = await _I18n.GetString("units", $"units.descriptions.{unit.DefinitionName}", cancel) ?? $"<missing desc>";

            if (string.IsNullOrEmpty(unit.Unit.ExplodeAs) == false) {
                Result<BarWeaponDefinition, string> wep = await _WeaponDefinitionRepository.GetWeaponDefinition(unit.Unit.ExplodeAs, cancel);
                if (wep.IsOk == true) {
                    unit.IncludedWeapons.Add(wep.Value);
                } else {
                    _Logger.LogWarning($"missing unit ExplodeAs weapon [defName={unit.DefinitionName}] [explodeAs={unit.Unit.ExplodeAs}] [error={wep.Error}]");
                }
            }
            if (unit.Unit.ExplodeAs != unit.Unit.SelfDestructWeapon && string.IsNullOrEmpty(unit.Unit.SelfDestructWeapon) == false) {
                Result<BarWeaponDefinition, string> wep = await _WeaponDefinitionRepository.GetWeaponDefinition(unit.Unit.SelfDestructWeapon, cancel);
                if (wep.IsOk == true) {
                    unit.IncludedWeapons.Add(wep.Value);
                } else {
                    _Logger.LogWarning($"missing unit SelfDestructWeapon weapon [defName={unit.DefinitionName}] "
                        + $"[selfDestructWeapon={unit.Unit.SelfDestructWeapon}] [error={wep.Error}]");
                }
            }

            foreach (BarUnitWeapon weapon in unit.Unit.Weapons) {
                if (weapon.WeaponDefinition.CarriedUnit != null) {
                    Result<BarUnit, string> carriedUnit = await _BarUnitRepository.GetByDefinitionName(weapon.WeaponDefinition.CarriedUnit.DefinitionName, cancel);
                    if (carriedUnit.IsOk == true) {
                        unit.IncludedUnits.Add(carriedUnit.Value);
                    } else {
                        _Logger.LogWarning($"failed to get BarUnit for carried weapon [defName={unit.DefinitionName}] "
                            + $"[carriedUnit={weapon.WeaponDefinition.CarriedUnit.DefinitionName}] [error={carriedUnit.Error}]");
                    }
                }
            }

            return unit;
        }

    }
}
