# Check getItemFlagId01 for all treasure-bound lots in the FogMod DS1R param
# to see if vanilla uses 0 or unique flags for ground pickups.
import sys, struct, os

# Read DS1R GameParam.parambnd.dcx using the python reference tools
sys.path.insert(0, r'Z:\Bonejam\VideoGameShop\dsr-mega\DarkSoulsItemRandomizer-master')
import dcx_handler, bnd_rebuilder, item_lot_param

bnd_path = r'Z:\Bonejam\VideoGameShop\dsr-mega\FogMod-master\dist\DS1R\param\GameParam\GameParam.parambnd.dcx'
raw = open(bnd_path,'rb').read()
bnd_data = dcx_handler.uncompress_dcx_content(raw)
files = bnd_rebuilder.unpack_bnd(bnd_data)
lot_bytes = next(d for n,d in files if 'ItemLotParam' in n)

ilp = item_lot_param.ItemLotParam(lot_bytes)

# Gather flag01 values for treasure-bound ranges (map-prefixed lots)
# These are the lots referenced by MSB Treasure events
flagvals = {}
for row in ilp.rows:
    lid = row['id']
    flag = row.get('getItemFlagId01', 0)
    if flag not in flagvals:
        flagvals[flag] = []
    flagvals[flag].append(lid)

# How many use flag 0 vs unique flags?
zero_flag = flagvals.get(0, [])
nonzero = {f:v for f,v in flagvals.items() if f != 0}
print(f"Lots with getItemFlagId01 = 0: {len(zero_flag)}")
print(f"Lots with getItemFlagId01 != 0: {sum(len(v) for v in nonzero.values())} (in {len(nonzero)} unique flags)")

# Sample treasure-range lots specifically (map lots: 1xxxxxx)
print("\nSample map-range lots and their flag01:")
for row in ilp.rows:
    lid = row['id']
    if 1000000 <= lid <= 1999990 and row.get('lotItemId01', 0) != 0:
        print(f"  {lid}: flag01={row.get('getItemFlagId01', '?')} id01={row.get('lotItemId01','?')}")
    if lid > 1020000:
        break
