import re
import matplotlib.pyplot as plt

log_files = [
    r"C:\Users\ryan7\Documents\FactionWars\Logs\FactionWars_2026-02-02_22-37-49.log",
    r"C:\Users\ryan7\Documents\FactionWars\Logs\FactionWars_2026-02-02_23-05-52.log",
]

faction_data = {
    'trevor': {'times': [], 'cash': [], 'troops': [], 'zones': []},
    'franklin': {'times': [], 'cash': [], 'troops': [], 'zones': []},
}
zone_changes = []

# Session start time: 22:37:49
START_MINUTES = 22 * 60 + 37 + 49/60

def parse_time(time_str):
    parts = time_str.split(':')
    hours = int(parts[0])
    minutes = int(parts[1])
    seconds = float(parts[2])
    total_minutes = hours * 60 + minutes + seconds / 60
    return total_minutes - START_MINUTES

for log_path in log_files:
    print(f"Reading {log_path}...")
    with open(log_path, 'r', encoding='utf-8') as f:
        for line in f:
            if 'state: Cash=' in line:
                time_match = re.search(r'\[(\d{2}:\d{2}:\d{2}\.\d{3})\]', line)
                state_match = re.search(r'(\w+) state: Cash=\$(\d+), Troops=(\d+), Zones=(\d+)', line)
                if time_match and state_match:
                    time_str = time_match.group(1)
                    faction = state_match.group(1).lower()
                    cash = int(state_match.group(2))
                    troops = int(state_match.group(3))
                    zones = int(state_match.group(4))
                    if faction in faction_data:
                        t = parse_time(time_str)
                        faction_data[faction]['times'].append(t)
                        faction_data[faction]['cash'].append(cash)
                        faction_data[faction]['troops'].append(troops)
                        faction_data[faction]['zones'].append(zones)

            if "): " in line and " -> " in line and "Zone '" in line:
                time_match = re.search(r'\[(\d{2}:\d{2}:\d{2}\.\d{3})\]', line)
                zone_match = re.search(r"Zone '([^']+)'.*: (\w+) -> (\w+)", line)
                if time_match and zone_match:
                    time_str = time_match.group(1)
                    zone_name = zone_match.group(1)
                    old_owner = zone_match.group(2).lower()
                    new_owner = zone_match.group(3).lower()
                    zone_changes.append((parse_time(time_str), zone_name, old_owner, new_owner))

# Michael zone tracking
michael_zones = {'times': [0], 'zones': [3]}
michael_zone_set = {'vinewood', 'rockford_hills', 'del_perro'}

for time, zone_name, old_owner, new_owner in sorted(zone_changes):
    zone_id = zone_name.lower().replace(' ', '_').replace("'", "")
    changed = False
    if new_owner == 'michael':
        if zone_id not in michael_zone_set:
            michael_zone_set.add(zone_id)
            changed = True
    elif old_owner == 'michael':
        if zone_id in michael_zone_set:
            michael_zone_set.discard(zone_id)
            changed = True
    if changed:
        michael_zones['times'].append(time)
        michael_zones['zones'].append(len(michael_zone_set))

print(f"\nTrevor data points: {len(faction_data['trevor']['times'])}")
print(f"Franklin data points: {len(faction_data['franklin']['times'])}")
print(f"Michael zone changes: {len(michael_zones['times'])}")
print(f"Time range: {min(faction_data['trevor']['times']):.1f} - {max(faction_data['trevor']['times']):.1f} min")

# Create figure
fig, axes = plt.subplots(3, 1, figsize=(16, 14))
fig.suptitle('Faction Wars - Complete Session Analysis\n2026-02-02 22:37 - 23:31 (54 minutes)',
             fontsize=14, fontweight='bold')

colors = {'trevor': '#FF6B00', 'franklin': '#00AA00', 'michael': '#0066CC'}

max_time = max(max(faction_data['trevor']['times']), max(faction_data['franklin']['times']))

# Plot 1: Troop Count
ax1 = axes[0]
ax1.plot(faction_data['trevor']['times'], faction_data['trevor']['troops'],
         color=colors['trevor'], linewidth=2.5, marker='o', markersize=4, label='Trevor')
ax1.plot(faction_data['franklin']['times'], faction_data['franklin']['troops'],
         color=colors['franklin'], linewidth=2.5, marker='s', markersize=4, label='Franklin')
ax1.axhline(y=20, color='gray', linestyle='--', alpha=0.5, linewidth=1)
ax1.set_ylabel('Troop Count', fontsize=11)
ax1.set_title('Troop Count Over Time', fontsize=12)
ax1.legend(loc='upper left')
ax1.grid(True, alpha=0.3)
ax1.set_xlim(0, max_time + 2)
ax1.set_ylim(0, 60)

# Annotate key events
ax1.annotate('Trevor peaks\nat 55 troops!', xy=(53.6, 55), xytext=(45, 50),
             arrowprops=dict(arrowstyle='->', color='orange', lw=1.5),
             fontsize=9, color='#FF6B00', fontweight='bold')

# Franklin elimination
franklin_zero_idx = None
for i, t in enumerate(faction_data['franklin']['troops']):
    if t == 0:
        franklin_zero_idx = i
        break
if franklin_zero_idx:
    ax1.annotate('Franklin hits\n0 troops!',
                 xy=(faction_data['franklin']['times'][franklin_zero_idx], 0),
                 xytext=(faction_data['franklin']['times'][franklin_zero_idx] - 8, 15),
                 arrowprops=dict(arrowstyle='->', color='green', lw=1.5),
                 fontsize=9, color='#00AA00', fontweight='bold')

# Plot 2: Cash
ax2 = axes[1]
ax2.plot(faction_data['trevor']['times'], faction_data['trevor']['cash'],
         color=colors['trevor'], linewidth=2.5, marker='o', markersize=4, label='Trevor')
ax2.plot(faction_data['franklin']['times'], faction_data['franklin']['cash'],
         color=colors['franklin'], linewidth=2.5, marker='s', markersize=4, label='Franklin')
ax2.axhspan(0, 200, alpha=0.15, color='red')
ax2.set_ylabel('Cash ($)', fontsize=11)
ax2.set_title('Cash Over Time', fontsize=12)
ax2.legend(loc='upper left')
ax2.grid(True, alpha=0.3)
ax2.set_xlim(0, max_time + 2)

# Plot 3: Zone Count
ax3 = axes[2]
ax3.plot(faction_data['trevor']['times'], faction_data['trevor']['zones'],
         color=colors['trevor'], linewidth=2.5, marker='o', markersize=4, label='Trevor')
ax3.plot(faction_data['franklin']['times'], faction_data['franklin']['zones'],
         color=colors['franklin'], linewidth=2.5, marker='s', markersize=4, label='Franklin')
ax3.step(michael_zones['times'], michael_zones['zones'],
         color=colors['michael'], linewidth=2.5, where='post', label='Michael (Player)')
ax3.set_ylabel('Zones Controlled', fontsize=11)
ax3.set_xlabel('Minutes Since Game Start', fontsize=11)
ax3.set_title('Zone Control Over Time', fontsize=12)
ax3.legend(loc='upper left')
ax3.grid(True, alpha=0.3)
ax3.set_xlim(0, max_time + 2)
ax3.set_ylim(0, 16)

# Annotate Franklin elimination
ax3.annotate('FRANKLIN\nELIMINATED!',
             xy=(53.6, 0), xytext=(45, 3),
             arrowprops=dict(arrowstyle='->', color='red', lw=2),
             fontsize=10, color='red', fontweight='bold',
             bbox=dict(boxstyle='round', facecolor='yellow', alpha=0.8))

ax3.annotate('Trevor dominates\nwith 13 zones',
             xy=(53.6, 13), xytext=(45, 11),
             arrowprops=dict(arrowstyle='->', color='orange', lw=1.5),
             fontsize=9, color='#FF6B00', fontweight='bold')

# Phase markers
for ax in axes:
    ax.axvline(x=28, color='red', linestyle='--', alpha=0.7, linewidth=2)
    ax.axvline(x=10, color='purple', linestyle=':', alpha=0.5, linewidth=1.5)

ax1.text(5, 55, 'Land Grab', ha='center', fontsize=10, color='purple', fontweight='bold')
ax1.text(19, 55, 'Downtown War', ha='center', fontsize=10, color='purple', fontweight='bold')
ax1.text(40, 55, 'Trevor Conquest', ha='center', fontsize=10, color='red', fontweight='bold')

plt.tight_layout()
plt.savefig(r'C:\Users\ryan7\programming\gtav-factions\tools\faction_analysis.png', dpi=150, bbox_inches='tight')
plt.savefig(r'C:\Users\ryan7\Documents\FactionWars\Logs\faction_analysis.png', dpi=150, bbox_inches='tight')
print("\nCharts saved!")

# Print final stats
print("\n=== FINAL STANDINGS ===")
print(f"Trevor: {faction_data['trevor']['zones'][-1]} zones, {faction_data['trevor']['troops'][-1]} troops, ${faction_data['trevor']['cash'][-1]}")
print(f"Franklin: {faction_data['franklin']['zones'][-1]} zones, {faction_data['franklin']['troops'][-1]} troops, ${faction_data['franklin']['cash'][-1]} - ELIMINATED!")
