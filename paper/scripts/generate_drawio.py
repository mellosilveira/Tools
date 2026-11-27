"""Generate native, editable Draw.io (.drawio) diagrams for the JOSS manuscript."""

from html import escape
from pathlib import Path
import xml.etree.ElementTree as ET


FIGURES_DIR = Path(__file__).resolve().parent.parent / "figures"

THEMES = {
    "host": ("#F1F5F9", "#475569"),
    "meta": ("#FFF7ED", "#EA580C"),
    "plugin": ("#FFF1F2", "#E11D48"),
    "optim": ("#FEFCE8", "#CA8A04"),
    "web": ("#FFF1F2", "#DC2626"),
    "mech": ("#F5F3FF", "#7C3AED"),
    "db": ("#F0FDFA", "#0D9488"),
    "math": ("#F3E8FF", "#6D28D9"),
    "core": ("#EFF6FF", "#2563EB"),
    "telemetry": ("#ECFDF5", "#0F766E"),
    "zone": ("#F8FAFC", "#94A3B8"),
    "zone_opt": ("#FFFEFA", "#CA8A04"),
}


class DrawioBuilder:
    def __init__(self, page_w: int, page_h: int, diagram_name: str):
        self.page_w = page_w
        self.page_h = page_h
        self.diagram_name = diagram_name
        self._id = 2
        self.cells: list[ET.Element] = []
        self.rects: dict[str, tuple[int, int, int, int]] = {}

    def _next_id(self) -> str:
        val = str(self._id)
        self._id += 1
        return val

    def add_title(self, x: int, y: int, w: int, text: str) -> None:
        cid = self._next_id()
        cell = ET.Element("mxCell", {
            "id": cid,
            "value": f"<b>{escape(text)}</b>",
            "style": (
                "text;html=1;align=center;verticalAlign=middle;resizable=0;"
                "points=[];autosize=0;strokeColor=none;fillColor=none;"
                "fontFamily=Helvetica;fontSize=15;fontColor=#0F172A;"
            ),
            "vertex": "1",
            "parent": "1",
        })
        ET.SubElement(cell, "mxGeometry", {
            "x": str(x), "y": str(y), "width": str(w), "height": "28", "as": "geometry",
        })
        self.cells.append(cell)

    def add_note(self, x: int, y: int, w: int, h: int, text: str) -> None:
        cid = self._next_id()
        cell = ET.Element("mxCell", {
            "id": cid,
            "value": escape(text),
            "style": (
                "text;html=1;align=left;verticalAlign=middle;resizable=0;"
                "strokeColor=none;fillColor=none;whiteSpace=wrap;"
                "fontFamily=Helvetica;fontSize=10;fontColor=#475569;"
            ),
            "vertex": "1",
            "parent": "1",
        })
        ET.SubElement(cell, "mxGeometry", {
            "x": str(x), "y": str(y), "width": str(w), "height": str(h), "as": "geometry",
        })
        self.cells.append(cell)

    def add_zone(self, x: int, y: int, w: int, h: int, label: str, theme: str = "zone") -> None:
        fill, stroke = THEMES[theme]
        cid = self._next_id()
        cell = ET.Element("mxCell", {
            "id": cid,
            "value": f"<b>{escape(label)}</b>",
            "style": (
                f"rounded=1;whiteSpace=wrap;html=1;arcSize=4;dashed=1;dashPattern=6 4;"
                f"fillColor={fill};strokeColor={stroke};strokeWidth=1.1;"
                "verticalAlign=top;align=left;spacingTop=6;spacingLeft=10;"
                "fontFamily=Helvetica;fontSize=11;fontColor=#475569;"
            ),
            "vertex": "1",
            "parent": "1",
        })
        ET.SubElement(cell, "mxGeometry", {
            "x": str(x), "y": str(y), "width": str(w), "height": str(h), "as": "geometry",
        })
        self.cells.append(cell)

    def add_box(
        self,
        x: int,
        y: int,
        w: int,
        h: int,
        title: str,
        subtitle: str = "",
        theme: str = "host",
        title_px: int = 11,
        sub_px: int = 9,
        dashed: bool = False,
    ) -> str:
        fill, stroke = THEMES[theme]
        cid = self._next_id()
        self.rects[cid] = (x, y, w, h)
        if subtitle:
            label = (
                f"<div style='line-height:1.22;'>"
                f"<span style='font-size:{title_px}px;font-weight:700;color:#0F172A;'>{escape(title)}</span>"
                f"<br/>"
                f"<span style='font-size:{sub_px}px;color:#475569;'>{escape(subtitle)}</span>"
                f"</div>"
            )
        else:
            label = f"<span style='font-size:{title_px}px;font-weight:700;color:#0F172A;'>{escape(title)}</span>"

        dash_style = "dashed=1;dashPattern=4 3;" if dashed else ""
        cell = ET.Element("mxCell", {
            "id": cid,
            "value": label,
            "style": (
                f"rounded=1;whiteSpace=wrap;html=1;arcSize=12;{dash_style}"
                f"fillColor={fill};strokeColor={stroke};strokeWidth=1.4;"
                "align=center;verticalAlign=middle;spacingLeft=6;spacingRight=6;"
                "fontFamily=Helvetica;"
            ),
            "vertex": "1",
            "parent": "1",
        })
        ET.SubElement(cell, "mxGeometry", {
            "x": str(x), "y": str(y), "width": str(w), "height": str(h), "as": "geometry",
        })
        self.cells.append(cell)
        return cid

    def add_bus_node(self, x: int, y: int, color: str = "#EA580C") -> str:
        """Small clean junction dot for single-trunk orthogonal fan-out."""
        cid = self._next_id()
        self.rects[cid] = (x - 3, y - 3, 6, 6)
        cell = ET.Element("mxCell", {
            "id": cid,
            "value": "",
            "style": f"ellipse;whiteSpace=wrap;html=1;aspect=fixed;fillColor={color};strokeColor=none;",
            "vertex": "1",
            "parent": "1",
        })
        ET.SubElement(cell, "mxGeometry", {
            "x": str(x - 3), "y": str(y - 3), "width": "6", "height": "6", "as": "geometry",
        })
        self.cells.append(cell)
        return cid

    def add_edge(
        self,
        src: str,
        dst: str,
        color: str = "#334155",
        dashed: bool = False,
        label: str = "",
        exit_pt: tuple[float, float] | None = None,
        entry_pt: tuple[float, float] | None = None,
        waypoints: list[tuple[int, int]] | None = None,
        end_arrow: str = "block",
        stroke_width: float = 1.35,
    ) -> str:
        cid = self._next_id()
        style_parts = [
            "edgeStyle=orthogonalEdgeStyle",
            "rounded=1",
            "orthogonalLoop=1",
            "jettySize=auto",
            "html=1",
            f"strokeColor={color}",
            f"strokeWidth={stroke_width}",
            f"endArrow={end_arrow}",
            "endFill=1" if end_arrow != "none" else "endFill=0",
            "fontFamily=Helvetica",
            "fontSize=9",
            f"fontColor={color}",
            "labelBackgroundColor=#FFFFFF",
        ]
        if dashed:
            style_parts.extend(["dashed=1", "dashPattern=5 3"])
        if exit_pt is not None:
            style_parts.extend([f"exitX={exit_pt[0]}", f"exitY={exit_pt[1]}", "exitDx=0", "exitDy=0"])
        if entry_pt is not None:
            style_parts.extend([f"entryX={entry_pt[0]}", f"entryY={entry_pt[1]}", "entryDx=0", "entryDy=0"])

        cell = ET.Element("mxCell", {
            "id": cid,
            "value": f"<b>{escape(label)}</b>" if label else "",
            "style": ";".join(style_parts) + ";",
            "edge": "1",
            "parent": "1",
            "source": src,
            "target": dst,
        })
        geom = ET.SubElement(cell, "mxGeometry", {"relative": "1", "as": "geometry"})
        if waypoints:
            arr = ET.SubElement(geom, "Array", {"as": "points"})
            for wx, wy in waypoints:
                ET.SubElement(arr, "mxPoint", {"x": str(wx), "y": str(wy)})
        self.cells.append(cell)
        return cid

    def align_entry_x(self, src_id: str, dst_id: str, exit_x: float = 0.5) -> float:
        """Compute exact normalized entryX on dst_id directly below src_id (zero jog)."""
        sx, _, sw, _ = self.rects[src_id]
        dx, _, dw, _ = self.rects[dst_id]
        px = sx + sw * exit_x
        return round((px - dx) / dw, 4)

    def write(self, path: Path) -> None:
        mxfile = ET.Element("mxfile", {
            "host": "MelloSilveiraTools",
            "agent": "generate_drawio.py",
            "version": "24.0.0",
        })
        diagram = ET.SubElement(mxfile, "diagram", {
            "id": self.diagram_name,
            "name": self.diagram_name,
        })
        model = ET.SubElement(diagram, "mxGraphModel", {
            "dx": str(self.page_w),
            "dy": str(self.page_h),
            "grid": "1",
            "gridSize": "10",
            "guides": "1",
            "tooltips": "1",
            "connect": "1",
            "arrows": "1",
            "fold": "1",
            "page": "1",
            "pageScale": "1",
            "pageWidth": str(self.page_w),
            "pageHeight": str(self.page_h),
            "background": "#FFFFFF",
            "math": "0",
            "shadow": "0",
        })
        root = ET.SubElement(model, "root")
        ET.SubElement(root, "mxCell", {"id": "0"})
        ET.SubElement(root, "mxCell", {"id": "1", "parent": "0"})
        for c in self.cells:
            root.append(c)

        ET.indent(mxfile, space="  ")
        path.parent.mkdir(parents=True, exist_ok=True)
        ET.ElementTree(mxfile).write(path, encoding="utf-8", xml_declaration=True)


def build_fig1_architecture() -> None:
    """Compact layered .NET 10 architecture with outer-rail meta-package aggregation (incl. Plugins) and clean corridors."""
    d = DrawioBuilder(920, 610, "PackageArchitecture")
    d.add_title(20, 8, 880, "MelloSilveiraTools: Modular .NET 10 Package & Execution Architecture")

    # Zones (concise Zone 2 title so Migrator drop clears all text!)
    d.add_zone(20, 44, 880, 108, "Turnkey Executables, CLI & Interactive Frontend", theme="zone")
    d.add_zone(20, 166, 880, 404, "8 AOT-Compatible NuGet Packages (.NET 10)", theme="zone")

    # Compact Top Host Row
    launcher = d.add_box(42, 96, 152, 42, "Launcher", "CLI & infra setup", theme="host", title_px=10, sub_px=8)
    migrator = d.add_box(246, 96, 164, 42, "Database.Migrator", "Schema & RLS runner", theme="host", title_px=10, sub_px=8)
    webhost = d.add_box(482, 96, 168, 42, "WebApi.Host", "REST, NDJSON & Swagger", theme="host", title_px=10, sub_px=8)
    webui = d.add_box(712, 96, 166, 42, "WebUi", "Blazor WASM + ECharts", theme="host", title_px=10, sub_px=8)

    # Meta-Package Row
    meta = d.add_box(335, 194, 250, 40, "MelloSilveiraTools (Meta-Package)", "Aggregates all 7 library packages + DI", theme="meta", title_px=10, sub_px=8)

    # Layer 1: High-level libraries
    plugins = d.add_box(72, 286, 162, 42, "Plugins", "Collectible runtime & hot-load", theme="plugin", title_px=10, sub_px=8)
    optim = d.add_box(462, 286, 224, 42, "MechanicsOfMaterials.Optimizations", "Segmentation, fitting & OAAT sweep", theme="optim", title_px=10, sub_px=8)

    # Layer 2: API, Biomechanics & Math
    webapi = d.add_box(72, 365, 162, 42, "WebApi", "Endpoints, JWE auth, NDJSON", theme="web", title_px=10, sub_px=8)
    mech = d.add_box(350, 365, 174, 42, "MechanicsOfMaterials", "Viscoelastic laws, fatigue, forces", theme="mech", title_px=10, sub_px=8)
    math = d.add_box(595, 365, 185, 42, "Mathematics", "Functions, Prony, Simpson, Brent", theme="math", title_px=10, sub_px=8)

    # Layer 3: Database + Full-width Foundation Core
    db = d.add_box(246, 444, 200, 42, "Database", "PostgreSQL repository, SQL, RLS", theme="db", title_px=10, sub_px=8)
    core = d.add_box(72, 514, 708, 40, "Core (Foundation)", "Result monad, caches, Polly v8 resilience, file storage abstraction, lossless dataflow", theme="core", title_px=10, sub_px=8)

    # Host connections
    d.add_edge(launcher, migrator, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))
    d.add_edge(launcher, webhost, exit_pt=(0.5, 0.0), entry_pt=(0.5, 0.0), waypoints=[(118, 76), (566, 76)])
    d.add_edge(webhost, webui, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))
    d.add_edge(webhost, meta, exit_pt=(0.45, 1.0), entry_pt=(0.89, 0.0))
    # Migrator drops straight through open whitespace at x=318 (well right of Zone 2 label!)
    d.add_edge(migrator, db, color="#64748B", dashed=True, exit_pt=(0.44, 1.0), entry_pt=(0.36, 0.0))

    # ---- CLEAN ORANGE META-PACKAGE AGGREGATION BUS (ZERO CENTER CLUTTER!) ----
    j_outer_l = d.add_bus_node(42, 256, "#EA580C")   # Outer-left rail for WebApi & Database!
    j_left = d.add_bus_node(153, 256, "#EA580C")     # Direct straight drop into Plugins!
    j_mech = d.add_bus_node(420, 256, "#EA580C")     # Clean drop into MechanicsOfMaterials!
    j_center = d.add_bus_node(460, 256, "#EA580C")   # Meta center drop
    j_opt = d.add_bus_node(574, 256, "#EA580C")      # Clean straight drop into Optimizations!
    j_math = d.add_bus_node(705, 256, "#EA580C")     # Clean drop past right of Optimizations into Mathematics!

    d.add_edge(meta, j_center, color="#EA580C", dashed=True, end_arrow="none", exit_pt=(0.5, 1.0), entry_pt=(0.5, 0.0))
    d.add_edge(j_center, j_mech, color="#EA580C", dashed=True, end_arrow="none", exit_pt=(0.0, 0.5), entry_pt=(1.0, 0.5))
    d.add_edge(j_mech, j_left, color="#EA580C", dashed=True, end_arrow="none", exit_pt=(0.0, 0.5), entry_pt=(1.0, 0.5))
    d.add_edge(j_left, j_outer_l, color="#EA580C", dashed=True, end_arrow="none", exit_pt=(0.0, 0.5), entry_pt=(1.0, 0.5))
    d.add_edge(j_center, j_opt, color="#EA580C", dashed=True, end_arrow="none", exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))
    d.add_edge(j_opt, j_math, color="#EA580C", dashed=True, end_arrow="none", exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))

    # Drops from orange bus to ALL 7 NuGet packages (explicitly connected to Plugins!)
    d.add_edge(j_outer_l, webapi, color="#EA580C", dashed=True, exit_pt=(0.5, 1.0), entry_pt=(0.0, 0.5), waypoints=[(42, 387)])
    d.add_edge(j_outer_l, db, color="#EA580C", dashed=True, exit_pt=(0.5, 1.0), entry_pt=(0.0, 0.65), waypoints=[(42, 471)])
    d.add_edge(j_left, plugins, color="#EA580C", dashed=True, exit_pt=(0.5, 1.0), entry_pt=(0.5, 0.0))
    d.add_edge(j_mech, mech, color="#EA580C", dashed=True, exit_pt=(0.5, 1.0), entry_pt=(0.40, 0.0))
    d.add_edge(j_opt, optim, color="#EA580C", dashed=True, exit_pt=(0.5, 1.0), entry_pt=(0.5, 0.0))
    d.add_edge(j_math, math, color="#EA580C", dashed=True, exit_pt=(0.5, 1.0), entry_pt=(0.59, 0.0))
    d.add_edge(meta, core, color="#EA580C", dashed=True, label="Bundles all 7", exit_pt=(1.0, 0.5), entry_pt=(1.0, 0.5), waypoints=[(835, 214), (835, 534)])

    # ---- DIRECT LIBRARY DEPENDENCIES (spacious uncluttered center corridor) ----
    d.add_edge(plugins, webapi, exit_pt=(0.5, 1.0), entry_pt=(0.5, 0.0))
    d.add_edge(plugins, db, exit_pt=(0.88, 1.0), entry_pt=(0.10, 0.0), waypoints=[(215, 342), (265, 342)])

    d.add_edge(optim, webapi, exit_pt=(0.0, 0.5), entry_pt=(1.0, 0.38), waypoints=[(290, 307), (290, 382)])
    d.add_edge(optim, mech, entry_pt=(0.75, 0.0))
    d.add_edge(optim, db, exit_pt=(0.25, 1.0), entry_pt=(0.5, 0.0), waypoints=[(518, 340), (345, 340)])

    d.add_edge(webapi, db, exit_pt=(0.74, 1.0), entry_pt=(0.0, 0.35), waypoints=[(192, 459)])
    d.add_edge(mech, db, entry_pt=(0.75, 0.0))
    d.add_edge(mech, math, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))

    # Laser-straight drops to Foundation Core bar
    d.add_edge(webapi, core, color="#2563EB", exit_pt=(0.35, 1.0), entry_pt=(d.align_entry_x(webapi, core, 0.35), 0.0))
    d.add_edge(db, core, color="#2563EB", exit_pt=(0.50, 1.0), entry_pt=(d.align_entry_x(db, core, 0.50), 0.0))
    d.add_edge(math, core, color="#2563EB", exit_pt=(0.50, 1.0), entry_pt=(d.align_entry_x(math, core, 0.50), 0.0))

    d.add_note(
        20, 578, 880, 24,
        "Notation: Solid arrows indicate compile-time package dependencies (A -> B means A depends on B). "
        "Orange dashed bus shows meta-package aggregation across all seven libraries (including Plugins).",
    )
    d.write(FIGURES_DIR / "fig1_package_architecture.drawio")


def build_fig2_experimental_pipeline() -> None:
    """Strictly horizontal experimental DAG with conceptual labels, laser-straight telemetry, and side-by-side parallel fork."""
    d = DrawioBuilder(1360, 420, "ExperimentalPipelineDAG")
    d.add_title(20, 10, 1320, "Experimental Data Processing: Horizontal Dataflow Pipeline & Live Progress Stream")

    d.add_zone(20, 48, 1320, 276, "Horizontal Bounded TPL Dataflow Execution Stages (Sequential + Lossless Parallel Branches)", theme="zone_opt")

    ingest = d.add_box(40, 146, 155, 68, "Input & Dispatch", "Paired CSV / sample load; non-blocking background run", theme="host")
    segment = d.add_box(225, 146, 175, 68, "Phase Segmentation", "Finite-difference classification: Ramp, Relaxation, Descent & Recovery", theme="optim")
    fanout = d.add_box(430, 157, 82, 46, "Broadcast", "Lossless split", theme="core", sub_px=8)

    csv_arch = d.add_box(398, 246, 146, 58, "Segmented Archive", "Save isolated phase CSVs to file storage", theme="db", title_px=10, sub_px=8)

    down_build = d.add_box(550, 146, 170, 68, "Downsample & Build", "Reduce point density & assemble relaxation curve", theme="math")
    fit = d.add_box(755, 146, 185, 68, "Constitutive Fitting", "Fung, Simplified Fung & Schapery (SHADE + L-BFGS)", theme="optim")
    fork = d.add_box(970, 157, 74, 46, "Fork", "2-way parallel", theme="core", sub_px=8)

    # TWO SIDE-BY-SIDE PARALLEL HORIZONTAL ROWS TO THE RIGHT OF FORK
    id_hash = d.add_box(1080, 76, 120, 62, "SHA-256 Keying", "Deterministic run fingerprint", theme="core", title_px=10, sub_px=8)
    save_fit = d.add_box(1222, 76, 102, 62, "Save Fit", "Fitted constants (PostgreSQL)", theme="db", title_px=10, sub_px=8)

    sim_fwd = d.add_box(1080, 214, 120, 62, "Forward Replay", "Simulate full stress history", theme="math", title_px=10, sub_px=8)
    save_sim = d.add_box(1222, 214, 102, 62, "Save Curve", "Predicted curves (PostgreSQL)", theme="db", title_px=10, sub_px=8)

    ndjson = d.add_box(
        40, 344, 1284, 52,
        "Live Progress & Diagnostics Stream (NDJSON -> Interactive Web UI)",
        "Non-blocking bounded channel broadcasts stage transitions, convergence metrics, and completion artifacts in real time",
        theme="telemetry",
        dashed=True,
    )

    d.add_edge(ingest, segment, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))
    d.add_edge(segment, fanout, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))
    d.add_edge(fanout, csv_arch, exit_pt=(0.5, 1.0), entry_pt=(d.align_entry_x(fanout, csv_arch, 0.5), 0.0))
    d.add_edge(fanout, down_build, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))
    d.add_edge(down_build, fit, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))
    d.add_edge(fit, fork, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))

    # Parallel horizontal branches
    d.add_edge(fork, id_hash, exit_pt=(1.0, 0.30), entry_pt=(0.0, 0.50), waypoints=[(1062, 171), (1062, 107)])
    d.add_edge(id_hash, save_fit, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))

    d.add_edge(fork, sim_fwd, exit_pt=(1.0, 0.70), entry_pt=(0.0, 0.50), waypoints=[(1062, 189), (1062, 245)])
    d.add_edge(sim_fwd, save_sim, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))

    # Exact laser-straight vertical telemetry taps (zero jog!)
    for box_id in (ingest, segment, down_build, fit):
        d.add_edge(box_id, ndjson, color="#0F766E", dashed=True, exit_pt=(0.5, 1.0), entry_pt=(d.align_entry_x(box_id, ndjson, 0.5), 0.0))

    d.write(FIGURES_DIR / "fig2_experimental_pipeline_dag.drawio")


def build_fig3_oaat_sweep() -> None:
    """Standardized horizontal OAAT parametric sweep matching Fig 2 grammar and exact vertical telemetry alignment."""
    d = DrawioBuilder(1260, 410, "ParametricSweepPipeline")
    d.add_title(20, 10, 1220, "One-At-A-Time (OAAT) Parametric Sensitivity: Horizontal Sweep & Dual-Sink Pipeline")

    d.add_zone(20, 48, 1220, 266, "Horizontal Bounded TPL Dataflow Sweep Stages (Dual Entry -> Parallel Output Fork)", theme="zone_opt")

    custom_in = d.add_box(40, 82, 185, 62, "Direct Parameter Input", "User-defined baseline vector & factor bounds", theme="host", title_px=10, sub_px=8)
    exp_in = d.add_box(40, 206, 185, 62, "Experimental Fit Lookup", "Load calibrated constants from database by ID", theme="db", title_px=10, sub_px=8)

    grid_gen = d.add_box(270, 142, 175, 68, "1D Parameter Grid", "Generate single-factor OAAT variations around baseline", theme="optim")
    sim_step = d.add_box(485, 142, 195, 68, "Simulation & Guard", "Evaluate stress relaxation & clip asymptote domain bounds", theme="math")
    fork = d.add_box(715, 153, 74, 46, "Fork", "2-way parallel", theme="core", sub_px=8)

    csv_out = d.add_box(830, 82, 175, 62, "Sweep CSV Export", "Write simulated curve family to file storage", theme="core", title_px=10, sub_px=8)
    db_out = d.add_box(830, 206, 175, 62, "Persist Sweep Run", "Aggregate batch metrics into PostgreSQL (RLS)", theme="db", title_px=10, sub_px=8)

    viz = d.add_box(1045, 142, 175, 68, "Interactive Sensitivity UI", "Apache ECharts multi-curve overlay & CSV export", theme="host")

    ndjson = d.add_box(
        40, 336, 1180, 52,
        "Live Progress & Bounds Stream (NDJSON -> Interactive Web UI)",
        "Non-blocking bounded channel reports per-factor sweep progress and asymptote-clipped strain limits in real time",
        theme="telemetry",
        dashed=True,
    )

    d.add_edge(custom_in, grid_gen, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.35), waypoints=[(248, 113), (248, 166)])
    d.add_edge(exp_in, grid_gen, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.65), waypoints=[(248, 237), (248, 186)])

    d.add_edge(grid_gen, sim_step, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))
    d.add_edge(sim_step, fork, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.5))

    d.add_edge(fork, csv_out, exit_pt=(1.0, 0.35), entry_pt=(0.0, 0.5), waypoints=[(810, 169), (810, 113)])
    d.add_edge(fork, db_out, exit_pt=(1.0, 0.65), entry_pt=(0.0, 0.5), waypoints=[(810, 183), (810, 237)])

    d.add_edge(csv_out, viz, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.35), waypoints=[(1025, 113), (1025, 166)])
    d.add_edge(db_out, viz, exit_pt=(1.0, 0.5), entry_pt=(0.0, 0.65), waypoints=[(1025, 237), (1025, 186)])

    # Exact laser-straight vertical telemetry taps & UI connection
    for box_id in (exp_in, grid_gen, sim_step):
        d.add_edge(box_id, ndjson, color="#0F766E", dashed=True, exit_pt=(0.5, 1.0), entry_pt=(d.align_entry_x(box_id, ndjson, 0.5), 0.0))
    d.add_edge(ndjson, viz, color="#0F766E", dashed=True, exit_pt=(d.align_entry_x(viz, ndjson, 0.5), 0.0), entry_pt=(0.5, 1.0))

    d.write(FIGURES_DIR / "fig3_oaat_sweep_pipeline.drawio")


def main() -> None:
    build_fig1_architecture()
    build_fig2_experimental_pipeline()
    build_fig3_oaat_sweep()
    print(f"Generated 3 Draw.io diagrams in {FIGURES_DIR}")


if __name__ == "__main__":
    main()
